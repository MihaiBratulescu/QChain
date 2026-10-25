using QChain.Internal;
using QChain.Internal.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Visitors;

// Keep the original calls and overloads; only substitute tuple representations.
internal sealed class TupleRewritingVisitor : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, ParameterExpression> parameters = [];

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is IShapedQuery shaped)
            return Visit(shaped.QueryShape.Source.Expression);

        if (node.Value is IQueryable query && query.Expression != node)
            return Visit(query.Expression);

        if (node.Value is null)
        {
            var type = CarrierProjection.RewriteType(node.Type);
            return type.IsValueType ? Expression.Default(type) : Expression.Constant(null, type);
        }

        if (node.Value is IQueryable source && CarrierProjection.RewriteType(source.ElementType) != source.ElementType)
            return CarrierProjection.Rebuild(node,
                typeof(IQueryable<>).MakeGenericType(CarrierProjection.RewriteType(source.ElementType)));

        return CarrierProjection.Lower(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (!parameters.TryGetValue(node, out var parameter))
        {
            var type = CarrierProjection.RewriteType(node.Type);
            parameters[node] = parameter = type == node.Type ? node : Expression.Parameter(type, node.Name);
        }
        return parameter;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var arguments = node.Parameters.Select(parameter => (ParameterExpression)Visit(parameter)).ToArray();
        var body = Visit(node.Body);
        return body == node.Body && arguments.SequenceEqual(node.Parameters) ? node :
            Expression.Lambda(CarrierProjection.RewriteType(node.Type), body, node.Name, node.TailCall, arguments);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        var arguments = node.Arguments.Select(argument => Visit(argument)).ToArray();
        if (CarrierProjection.IsTuple(node.Type))
        {
            if (arguments.Length == 0)
                return VisitDefault(Expression.Default(node.Type));
            return CarrierProjection.Create(arguments.Length == 8
                ? [.. arguments.Take(7), .. CarrierProjection.ReadItems(arguments[7],
                    CarrierProjection.TupleItemTypes(node.Type.GetGenericArguments()[7]).Length)]
                : arguments);
        }

        var type = CarrierProjection.RewriteType(node.Type);
        if (Nullable.GetUnderlyingType(node.Type) is { } underlying && CarrierProjection.IsTuple(underlying))
            return arguments.Length == 0 ? Expression.Default(type) : CarrierProjection.NullableValue(arguments[0]);

        if (node.Constructor is null)
            return Expression.New(type);

        var constructor = type == node.Type ? node.Constructor : type.GetConstructors()
            .Single(candidate => candidate.MetadataToken == node.Constructor.MetadataToken);
        var expected = constructor.GetParameters();
        arguments = arguments.Select((argument, index) => CarrierProjection.Rebuild(argument, expected[index].ParameterType)).ToArray();
        return node.Members is null ? Expression.New(constructor, arguments) :
            Expression.New(constructor, arguments, node.Members.Select(member => RebindMember(member, type)));
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var target = Visit(node.Expression);
        if (node.Expression is not null && CarrierProjection.IsTuple(node.Expression.Type))
        {
            var items = CarrierProjection.ReadItems(target!, CarrierProjection.TupleItemTypes(node.Expression.Type).Length);
            if (node.Member.Name == "Rest")
                return node.Type == typeof(ValueTuple) ? Expression.Default(node.Type) :
                    CarrierProjection.Create(items.Skip(7).ToArray());
            return items[int.Parse(node.Member.Name.AsSpan(4)) - 1];
        }
        var member = target is not null && target.Type != node.Expression!.Type
            ? RebindMember(node.Member, target.Type) : node.Member;
        // Value has a presence check; inlining its initializer would discard that check.
        if (member.Name == nameof(DefaultIfEmptyValue<int>.Value) &&
            member.DeclaringType is { IsGenericType: true } declaring && declaring.GetGenericTypeDefinition() == typeof(DefaultIfEmptyValue<>))
            return Expression.MakeMemberAccess(target, member);
        if (target is not null && ProjectionReduction.TryInlineMemberAccess(target, member, out var value))
            return value;
        return CarrierProjection.Lower(Expression.MakeMemberAccess(target, member));
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        var rewritten = Visit(node.NewExpression);
        if (CarrierProjection.IsTuple(node.Type))
        {
            var items = CarrierProjection.ReadItems(rewritten, CarrierProjection.TupleItemTypes(node.Type).Length);
            foreach (var assignment in node.Bindings.Cast<MemberAssignment>())
            {
                var value = Visit(assignment.Expression);
                if (assignment.Member.Name == "Rest")
                    CarrierProjection.ReadItems(value, items.Length - 7).CopyTo(items, 7);
                else
                    items[int.Parse(assignment.Member.Name.AsSpan(4)) - 1] = value;
            }
            return CarrierProjection.Create(items);
        }

        var created = (NewExpression)rewritten;
        return Expression.MemberInit(created, node.Bindings.Select(binding => VisitBinding(binding, created.Type)));
    }

    private MemberBinding VisitBinding(MemberBinding binding, Type type)
    {
        var member = RebindMember(binding.Member, type);
        return binding switch
        {
            MemberAssignment assignment => Expression.Bind(member,
                CarrierProjection.Rebuild(Visit(assignment.Expression), MemberType(member))),
            MemberMemberBinding nested => Expression.MemberBind(member,
                nested.Bindings.Select(child => VisitBinding(child, MemberType(member)))),
            MemberListBinding list => Expression.ListBind(member,
                list.Initializers.Select(initializer => VisitElementInit(initializer, MemberType(member)))),
            _ => throw new NotSupportedException($"Unsupported member binding: {binding.BindingType}.")
        };
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object is not null && node.Method.Name == nameof(Nullable<int>.GetValueOrDefault) &&
            node.Arguments.Count == 0 && Nullable.GetUnderlyingType(node.Object.Type) is { } tuple &&
            CarrierProjection.IsTuple(tuple))
        {
            var nullable = Visit(node.Object);
            return Expression.Condition(Expression.Property(nullable, "HasValue"),
                Expression.Property(nullable, "Value"), VisitDefault(Expression.Default(tuple)));
        }

        var arguments = node.Arguments.Select(argument => Visit(argument)).ToArray();
        if (node.Method.DeclaringType == typeof(ValueTuple) && node.Method.Name == nameof(ValueTuple.Create) && arguments.Length > 0)
            return CarrierProjection.Create(arguments);

        if (node.Method.DeclaringType is { } declaring && (declaring == typeof(Queryable) || declaring == typeof(Enumerable)) &&
            node.Method.Name == nameof(Queryable.DefaultIfEmpty) && arguments.Length == 1 &&
            CarrierProjection.SequenceElement(node.Arguments[0].Type) is { } element && CarrierProjection.IsTuple(element))
            return DefaultTuple(arguments[0], declaring, element);

        var target = Visit(node.Object);
        var method = UsesRewrittenTypes(node) ? RebindMethod(node.Method) : node.Method;
        if (target is not null)
            target = CarrierProjection.Rebuild(target, method.DeclaringType!);
        var expected = method.GetParameters();
        arguments = arguments.Select((argument, index) => CarrierProjection.Rebuild(argument, expected[index].ParameterType)).ToArray();
        return CarrierProjection.Lower(Expression.Call(target, method, arguments));
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);
        if (node.NodeType == ExpressionType.Quote)
            return Expression.Quote((LambdaExpression)operand);
        if (node.Method is not null)
            return CarrierProjection.Lower(Expression.MakeUnary(node.NodeType,
                CarrierProjection.Rebuild(operand, node.Operand.Type), node.Type, node.Method));
        var type = CarrierProjection.RewriteType(node.Type);
        if (node.NodeType == ExpressionType.TypeAs && type != node.Type)
            return CarrierProjection.Lower(Expression.TypeAs(CarrierProjection.Rebuild(operand, node.Operand.Type), node.Type));
        if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
        {
            if (operand.Type == type)
                return operand;
            if (Nullable.GetUnderlyingType(node.Type) is { } underlying && CarrierProjection.IsTuple(underlying))
            {
                if (IsNull(node.Operand))
                    return Expression.Default(type);
                if (CarrierProjection.IsTuple(node.Operand.Type))
                    return CarrierProjection.NullableValue(operand);
            }
            if (Nullable.GetUnderlyingType(node.Operand.Type) is { } sourceTuple && CarrierProjection.IsTuple(sourceTuple) &&
                node.Type == sourceTuple)
                return Expression.Property(operand, "Value");
            if (type == node.Type)
                operand = CarrierProjection.Rebuild(operand, node.Operand.Type);
            else if (operand.Type == node.Operand.Type)
                return CarrierProjection.Lower(Expression.MakeUnary(node.NodeType, operand, node.Type));
        }
        return Expression.MakeUnary(node.NodeType, operand, type);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (RewriteNullableComparison(node) is { } comparison)
            return comparison;

        var left = Visit(node.Left);
        var right = Visit(node.Right);
        if (node.NodeType == ExpressionType.Coalesce && Nullable.GetUnderlyingType(node.Left.Type) is { } tuple &&
            CarrierProjection.IsTuple(tuple))
            return RewriteTupleCoalesce(node, left, right);

        if (node.Method is not null)
        {
            left = CarrierProjection.Rebuild(left, node.Left.Type);
            right = CarrierProjection.Rebuild(right, node.Right.Type);
        }
        return CarrierProjection.Lower(Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull,
            node.Method, (LambdaExpression?)Visit(node.Conversion)));
    }

    private Expression? RewriteNullableComparison(BinaryExpression node)
    {
        if (node.NodeType is not (ExpressionType.Equal or ExpressionType.NotEqual))
            return null;

        var nullable = IsNull(node.Left) ? node.Right : IsNull(node.Right) ? node.Left : null;
        if (nullable is null || Nullable.GetUnderlyingType(nullable.Type) is not { } underlying || !CarrierProjection.IsTuple(underlying))
            return null;

        var hasValue = Expression.Property(Visit(nullable), "HasValue");
        return node.NodeType == ExpressionType.Equal ? Expression.Not(hasValue) : hasValue;
    }

    private Expression RewriteTupleCoalesce(BinaryExpression node, Expression left, Expression right)
    {
        var conversion = (LambdaExpression?)Visit(node.Conversion);
        var expected = conversion?.Parameters[0].Type ?? CarrierProjection.RewriteType(node.Type);
        Expression value = left.Type == expected ? left : Expression.Property(left, "Value");
        if (conversion is not null)
            value = Expression.Invoke(conversion, value);
        return Expression.Condition(Expression.Property(left, "HasValue"), value, right);
    }

    private static bool IsNull(Expression expression) => expression is ConstantExpression { Value: null } ||
        expression is DefaultExpression && Nullable.GetUnderlyingType(expression.Type) is not null;

    protected override Expression VisitConditional(ConditionalExpression node) => Expression.Condition(
        Visit(node.Test), Visit(node.IfTrue), Visit(node.IfFalse), CarrierProjection.RewriteType(node.Type));

    protected override Expression VisitDefault(DefaultExpression node) => CarrierProjection.IsTuple(node.Type)
        ? CarrierProjection.Create(CarrierProjection.TupleItemTypes(node.Type).Select(type => Visit(Expression.Default(type))).ToArray())
        : Expression.Default(CarrierProjection.RewriteType(node.Type));

    protected override Expression VisitNewArray(NewArrayExpression node) => node.NodeType == ExpressionType.NewArrayInit
        ? Expression.NewArrayInit(CarrierProjection.RewriteType(node.Type.GetElementType()!), node.Expressions.Select(expression => Visit(expression)))
        : CarrierProjection.Lower(node.Update(node.Expressions.Select(expression => Visit(expression))));

    protected override ElementInit VisitElementInit(ElementInit node) =>
        VisitElementInit(node, CarrierProjection.RewriteType(node.AddMethod.DeclaringType!));

    private ElementInit VisitElementInit(ElementInit node, Type collectionType)
    {
        var method = node.AddMethod.DeclaringType!.IsAssignableFrom(collectionType) ? node.AddMethod : RebindMethod(node.AddMethod);
        var expected = method.GetParameters();
        return Expression.ElementInit(method, node.Arguments.Select((argument, index) =>
            CarrierProjection.Rebuild(Visit(argument), expected[index].ParameterType)));
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        var target = Visit(node.Object);
        var indexer = node.Indexer is null ? null : (PropertyInfo)RebindMember(node.Indexer, target!.Type);
        var expected = indexer?.GetIndexParameters();
        var arguments = node.Arguments.Select((argument, index) =>
            CarrierProjection.Rebuild(Visit(argument), expected?[index].ParameterType ?? argument.Type));
        return CarrierProjection.Lower(Expression.MakeIndex(target!, indexer, arguments));
    }

    private static MemberInfo RebindMember(MemberInfo member, Type type) => member.DeclaringType == type ? member :
        type.GetMember(member.Name, member.MemberType,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Single();

    private static Type MemberType(MemberInfo member) => member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;

    private static bool IsSpan(Type type) => type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(Span<>) || type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>));

    private static bool UsesRewrittenTypes(MethodCallExpression node) =>
        node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable) ||
        node.Method.DeclaringType == typeof(MemoryExtensions) || IsSpan(node.Method.DeclaringType!) ||
        typeof(IQueryable).IsAssignableFrom(node.Type);

    private static MethodInfo RebindMethod(MethodInfo method)
    {
        var arguments = method.IsGenericMethod ? method.GetGenericArguments().Select(CarrierProjection.RewriteType).ToArray() : null;
        var declaring = CarrierProjection.RewriteType(method.DeclaringType!);
        if (declaring != method.DeclaringType)
            method = declaring.GetMethods().Single(candidate => candidate.MetadataToken == method.MetadataToken && candidate.Module == method.Module);
        return arguments is null ? method : method.GetGenericMethodDefinition().MakeGenericMethod(arguments);
    }

    // A reference carrier must retain default(ValueTuple)'s component defaults.
    private static Expression DefaultTuple(Expression source, Type linq, Type tupleType)
    {
        var carrierType = CarrierProjection.RewriteType(tupleType);
        var markerType = typeof(DefaultIfEmptyValue<>).MakeGenericType(carrierType);
        var item = Expression.Parameter(carrierType, "item");
        var wrap = Expression.MemberInit(Expression.New(markerType),
            Expression.Bind(markerType.GetProperty(nameof(DefaultIfEmptyValue<int>.HasValue))!, Expression.Constant(true)),
            Expression.Bind(markerType.GetProperty(nameof(DefaultIfEmptyValue<int>.Value))!, item));
        Expression Select(Expression sequence, LambdaExpression selector) => Expression.Call(linq, nameof(Queryable.Select),
            [selector.Parameters[0].Type, selector.ReturnType], sequence, linq == typeof(Queryable) ? Expression.Quote(selector) : selector);
        var wrapped = Select(source, Expression.Lambda(wrap, item));
        var defaulted = Expression.Call(linq, nameof(Queryable.DefaultIfEmpty), [markerType], wrapped);
        var row = Expression.Parameter(markerType, "row");
        var hasValue = Expression.Property(row, nameof(DefaultIfEmptyValue<int>.HasValue));
        var value = Expression.Property(row, nameof(DefaultIfEmptyValue<int>.Value));
        return Select(defaulted, Expression.Lambda(DefaultItems(value, tupleType, hasValue), row));
    }

    private static Expression DefaultItems(Expression value, Type publicType, Expression hasValue)
    {
        if (!CarrierProjection.IsTuple(publicType))
            return Expression.Condition(hasValue, value, Expression.Default(value.Type));

        var types = CarrierProjection.TupleItemTypes(publicType);
        return CarrierProjection.Create(CarrierProjection.ReadItems(value, types.Length)
            .Select((item, index) => DefaultItems(item, types[index], hasValue)).ToArray());
    }
}
