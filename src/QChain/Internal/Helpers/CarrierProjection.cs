using QChain.Internal.Types;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace QChain.Internal.Helpers;

internal static class CarrierProjection
{
    public static bool IsTuple(Type type) => type.IsGenericType &&
        type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    public static bool IsCarrier(Type type) => type.IsGenericType &&
        Definitions.Contains(type.GetGenericTypeDefinition());

    public static Type RewriteType(Type type)
    {
        if (type.IsArray)
        {
            var element = RewriteType(type.GetElementType()!);
            return element == type.GetElementType() ? type :
                type.IsSZArray ? element.MakeArrayType() : element.MakeArrayType(type.GetArrayRank());
        }

        if (!type.IsGenericType)
            return type;

        if (Nullable.GetUnderlyingType(type) is { } underlying && IsTuple(underlying))
            return typeof(DefaultIfEmptyValue<>).MakeGenericType(RewriteType(underlying));

        if (IsTuple(type))
            return Pack(TupleItemTypes(type).Select(RewriteType).ToArray(),
                items => Definitions[items.Count - 1].MakeGenericType(items.ToArray()));

        var arguments = type.GetGenericArguments();
        var rewritten = arguments.Select(RewriteType).ToArray();
        return arguments.SequenceEqual(rewritten) ? type : type.GetGenericTypeDefinition().MakeGenericType(rewritten);
    }

    public static MemberInitExpression Create(IReadOnlyList<Expression> items) =>
        (MemberInitExpression)Pack(items, CreateCarrier);

    private static Expression CreateCarrier(IReadOnlyList<Expression> items)
    {
        var type = Definitions[items.Count - 1].MakeGenericType(items.Select(item => item.Type).ToArray());
        return Expression.MemberInit(Expression.New(type),
            items.Select((item, index) => Expression.Bind(type.GetProperty($"Item{index + 1}")!, item)));
    }

    private static T Pack<T>(IReadOnlyList<T> items, Func<IReadOnlyList<T>, T> combine)
    {
        ArgumentOutOfRangeException.ThrowIfZero(items.Count);
        while (items.Count > 7)
            items = items.Chunk(7).Select(group => group.Length == 1 ? group[0] : combine(group)).ToArray();
        return combine(items);
    }

    // Only the CLR's Rest field continues a flat tuple; a tuple-valued Item stays one item.
    public static Type[] TupleItemTypes(Type type)
    {
        var arguments = type.GetGenericArguments();
        return arguments.Length == 8 ? [.. arguments.Take(7), .. TupleItemTypes(arguments[7])] : arguments;
    }

    public static Expression[] ReadItems(Expression carrier, int count)
    {
        if (count <= 7)
            return Enumerable.Range(0, count).Select(index => ReadSlot(carrier, $"Item{index + 1}")).ToArray();

        var groupSize = 7;
        while ((count - 1) / groupSize >= 7)
            groupSize = checked(groupSize * 7);

        return Enumerable.Range(0, (count - 1) / groupSize + 1).SelectMany(index =>
        {
            var group = ReadSlot(carrier, $"Item{index + 1}");
            var size = Math.Min(groupSize, count - index * groupSize);
            return size == 1 ? [group] : ReadItems(group, size);
        }).ToArray();
    }

    public static Expression ReadSlot(Expression target, string name)
    {
        var member = Expression.PropertyOrField(target, name);
        return ProjectionReduction.TryInlineMemberAccess(target, member.Member, out var value) ? value : member;
    }

    private static Expression[] ReadTuple(Expression tuple)
    {
        var count = tuple.Type.GetGenericArguments().Length;
        var items = Enumerable.Range(0, Math.Min(7, count)).Select(index => ReadSlot(tuple, $"Item{index + 1}"));
        return count == 8 ? [.. items, .. ReadTuple(ReadSlot(tuple, "Rest"))] : items.ToArray();
    }

    private static Expression ConstructTuple(Type type, IReadOnlyList<Expression> items)
    {
        var types = type.GetGenericArguments();
        if (types.Length == 0)
            return Expression.New(type);
        var arguments = types.Length == 8
            ? items.Take(7).Append(ConstructTuple(types[7], items.Skip(7).ToArray()))
            : items;
        return Expression.New(type.GetConstructor(types)!, arguments);
    }

    public static Expression Lower(Expression expression) => Convert(expression, RewriteType(expression.Type));
    public static Expression Rebuild(Expression expression, Type targetType) => Convert(expression, targetType);

    public static MemberInitExpression NullableValue(Expression value)
    {
        var type = typeof(DefaultIfEmptyValue<>).MakeGenericType(value.Type);
        return Expression.MemberInit(Expression.New(type),
            Expression.Bind(type.GetProperty("HasValue")!, Expression.Constant(true)),
            Expression.Bind(type.GetProperty("Value")!, value));
    }

    // Conversion is needed only at public boundaries: roots, captured values, and final results.
    private static Expression Convert(Expression expression, Type targetType)
    {
        if (expression.Type == targetType || targetType.IsAssignableFrom(expression.Type))
            return expression;

        if (Nullable.GetUnderlyingType(targetType) is { } targetTuple && IsTuple(targetTuple))
            return Expression.Condition(Expression.Property(expression, "HasValue"),
                Expression.Convert(Convert(Expression.Property(expression, "Value"), targetTuple), targetType), Expression.Default(targetType));

        if (Nullable.GetUnderlyingType(expression.Type) is { } sourceTuple && IsTuple(sourceTuple))
        {
            var hasValue = Expression.Property(expression, "HasValue");
            var value = Lower(Expression.Property(expression, "Value"));
            return Expression.MemberInit(Expression.New(targetType),
                Expression.Bind(targetType.GetProperty("HasValue")!, hasValue),
                Expression.Bind(targetType.GetProperty("Value")!, Expression.Condition(hasValue, value, Expression.Default(value.Type))));
        }

        if (IsTuple(expression.Type))
            return Create(ReadTuple(expression).Select(Lower).ToArray());

        if (IsTuple(targetType))
        {
            var types = TupleItemTypes(targetType);
            var items = ReadItems(expression, types.Length)
                .Select((item, index) => Convert(item, types[index])).ToArray();
            return ConstructTuple(targetType, items);
        }

        if (IsAnonymous(targetType))
        {
            var constructor = targetType.GetConstructors().Single();
            var members = constructor.GetParameters().Select(parameter => targetType.GetProperty(parameter.Name!)!).ToArray();
            return Expression.New(constructor,
                members.Select(member => Convert(Expression.Property(expression, member.Name), member.PropertyType)), members);
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            var types = targetType.GetGenericArguments();
            var items = ConvertSequence(expression, typeof(IEnumerable<>).MakeGenericType(types[1]));
            return Expression.New(typeof(ProjectedGrouping<,>).MakeGenericType(types).GetConstructors().Single(),
                Convert(Expression.Property(expression, "Key"), types[0]), items);
        }

        if (SequenceElement(targetType) is not null)
            return ConvertSequence(expression, targetType);

        throw new NotSupportedException($"Cannot substitute tuples in '{targetType}'.");
    }

    private static Expression ConvertSequence(Expression expression, Type targetType)
    {
        var sourceElement = SequenceElement(expression.Type)!;
        var targetElement = SequenceElement(targetType)!;
        if (expression.Type.IsSZArray && targetType.IsSZArray)
            return Expression.Call(typeof(QueryShape<,>).MakeGenericType(targetElement, sourceElement),
                nameof(QueryShape<object, object>.ProjectArray), Type.EmptyTypes, expression);

        var item = Expression.Parameter(sourceElement, "item");
        var selector = Expression.Lambda(Convert(item, targetElement), item);
        var queryable = typeof(IQueryable).IsAssignableFrom(targetType);
        Expression result = Expression.Call(queryable ? typeof(Queryable) : typeof(Enumerable), nameof(Enumerable.Select),
            [sourceElement, targetElement], expression, queryable ? Expression.Quote(selector) : selector);

        if (targetType.IsArray || targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            result = Expression.Call(typeof(Enumerable), targetType.IsArray ? nameof(Enumerable.ToArray) : nameof(Enumerable.ToList),
                [targetElement], result);

        if (!targetType.IsAssignableFrom(result.Type))
            throw new NotSupportedException($"Cannot substitute tuples in sequence '{targetType}'.");
        return result;
    }

    public static Type? SequenceElement(Type type) => type.GetInterfaces().Prepend(type)
        .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        ?.GetGenericArguments()[0];

    public static bool IsAnonymous(Type type) => type.IsDefined(typeof(CompilerGeneratedAttribute), false) &&
        type.Name.Contains("AnonymousType", StringComparison.Ordinal);

    private static readonly Type[] Definitions =
    [
        typeof(Carrier<>), typeof(Carrier<,>), typeof(Carrier<,,>), typeof(Carrier<,,,>),
        typeof(Carrier<,,,,>), typeof(Carrier<,,,,,>), typeof(Carrier<,,,,,,>)
    ];
}
