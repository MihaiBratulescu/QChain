using QChain.Internal.Helpers;
using QChain.Internal.Types;
using System.Collections;
using System.Linq.Expressions;

namespace QChain.Internal.Visitors;

// Reduce provider-facing tuple shapes to scalar expressions before execution.
internal sealed class ProviderTupleVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var contains = node.Method.Name == nameof(Enumerable.Contains) &&
            (node.Method.DeclaringType == typeof(Enumerable) || node.Method.DeclaringType == typeof(MemoryExtensions) ||
             node.Method.DeclaringType is { IsGenericType: true } declaring &&
             declaring.GetGenericTypeDefinition() == typeof(List<>));
        var any = node.Method.DeclaringType == typeof(Enumerable) && node.Method.Name == nameof(Enumerable.Any);
        if ((contains || any) && node.Arguments.Count == (node.Object is null ? 2 : 1))
        {
            var source = node.Object ?? node.Arguments[0];
            if (source is MethodCallExpression { Method.Name: "op_Implicit", Arguments.Count: 1 } conversion &&
                source.Type.IsByRefLike)
                source = conversion.Arguments[0];

            var element = CarrierProjection.SequenceElement(source.Type);
            var capture = new CaptureVisitor();
            capture.Visit(source);
            if (element is not null && (CarrierProjection.IsTuple(element) || CarrierProjection.IsCarrier(element)) &&
                !capture.HasFreeParameter && !capture.HasQuery)
            {
                // Read at execution, so replacement, additions, and removals remain observable.
                var values = Expression.Lambda<Func<IEnumerable>>(Expression.Convert(source, typeof(IEnumerable))).Compile()();
                var argument = node.Arguments[^1];
                var predicate = any ? (LambdaExpression)argument : null;
                Expression result = Expression.Constant(false);
                foreach (var value in values)
                {
                    var item = Expression.Constant(value, element);
                    var comparison = predicate is null ? Equal(Visit(argument), item) :
                        Visit(new ReplaceParameter(predicate.Parameters[0], item).Visit(predicate.Body));
                    result = Expression.OrElse(result, comparison);
                }
                return result;
            }
        }
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var test = Visit(node.Test);
        var whenTrue = Visit(node.IfTrue);
        var whenFalse = Visit(node.IfFalse);
        return Condition(test, whenTrue, whenFalse);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var target = Visit(node.Expression);
        if (target is not null && IsShape(target.Type))
            return Slot(target, node.Member.Name);
        return node.Update(target);
    }

    private static Expression Condition(Expression test, Expression whenTrue, Expression whenFalse)
    {
        if (test is ConstantExpression { Value: bool value })
            return value ? whenTrue : whenFalse;
        if (!IsShape(whenTrue.Type))
            return Expression.Condition(test, whenTrue, whenFalse);

        var type = whenTrue.Type;
        return Expression.MemberInit(Expression.New(type), type.GetProperties().Select(property =>
            Expression.Bind(property, Condition(test, Slot(whenTrue, property.Name), Slot(whenFalse, property.Name)))));
    }

    private static Expression Slot(Expression target, string name)
    {
        var property = target.Type.GetProperty(name)!;
        if (target is DefaultExpression or ConstantExpression { Value: null })
            return Expression.Default(property.PropertyType);
        if (ProjectionReduction.TryInlineMemberAccess(target, property, out var value))
            return value;
        return Expression.Property(target, property);
    }

    private static bool IsShape(Type type) => CarrierProjection.IsCarrier(type) ||
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefaultIfEmptyValue<>);

    private Expression Equal(Expression left, Expression right)
    {
        left = TupleValue(left);
        right = TupleValue(right);
        if (!CarrierProjection.IsCarrier(left.Type))
            return Expression.Equal(left, right);

        return left.Type.GetProperties().Select(property => Equal(
            Visit(Slot(left, property.Name)), Visit(Slot(right, property.Name))))
            .Aggregate(Expression.AndAlso);
    }

    private static Expression TupleValue(Expression value)
    {
        while (value is MethodCallExpression { Method.Name: nameof(QueryShape<object, object>.ProjectValue) } call &&
            call.Method.DeclaringType is { IsGenericType: true } declaring &&
            declaring.GetGenericTypeDefinition() == typeof(QueryShape<,>))
            value = call.Arguments[0];
        return CarrierProjection.IsTuple(value.Type) ? CarrierProjection.Lower(value) : value;
    }

    private sealed class ReplaceParameter(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == parameter ? replacement : node;
    }

    private sealed class CaptureVisitor : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> bound = [];
        public bool HasFreeParameter { get; private set; }
        public bool HasQuery { get; private set; }

        public override Expression? Visit(Expression? node)
        {
            HasQuery |= node is not null && typeof(IQueryable).IsAssignableFrom(node.Type);
            return base.Visit(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            HasFreeParameter |= !bound.Contains(node);
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var added = node.Parameters.Where(bound.Add).ToArray();
            Visit(node.Body);
            foreach (var parameter in added)
                bound.Remove(parameter);
            return node;
        }
    }
}
