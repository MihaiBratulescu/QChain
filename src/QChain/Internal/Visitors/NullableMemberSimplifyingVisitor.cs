using System.Linq.Expressions;

namespace QChain.Internal.Visitors;

internal sealed class NullableMemberSimplifyingVisitor : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        var target = Visit(node.Expression);
        if (target is null ||
            Nullable.GetUnderlyingType(target.Type) is null)
        {
            return node.Update(target);
        }

        if (node.Member.Name == nameof(Nullable<int>.HasValue) &&
            TryHasValue(target, out var hasValue))
        {
            return hasValue;
        }

        if (node.Member.Name == nameof(Nullable<int>.Value) &&
            TryValue(target, out var value))
        {
            return value;
        }

        return node.Update(target);
    }

    private static bool TryHasValue(Expression expression, out Expression hasValue)
    {
        hasValue = null!;

        if (expression is ConditionalExpression condition)
        {
            if (IsNullableNull(condition.IfFalse) && IsKnownNotNull(condition.IfTrue))
            {
                hasValue = condition.Test;
                return true;
            }

            if (IsNullableNull(condition.IfTrue) && IsKnownNotNull(condition.IfFalse))
            {
                hasValue = Expression.Not(condition.Test);
                return true;
            }
        }

        if (IsKnownNotNull(expression))
        {
            hasValue = Expression.Constant(true);
            return true;
        }

        return false;
    }

    private static bool TryValue(Expression expression, out Expression value)
    {
        value = null!;

        if (expression is NewExpression { Arguments.Count: 1 } nullable)
        {
            value = nullable.Arguments[0];
            return true;
        }

        if (expression is ConditionalExpression condition)
        {
            if (IsKnownNotNull(condition.IfTrue) && TryValue(condition.IfTrue, out value))
                return true;

            if (IsKnownNotNull(condition.IfFalse) && TryValue(condition.IfFalse, out value))
                return true;
        }

        return false;
    }

    private static bool IsNullableNull(Expression expression) =>
        expression is ConstantExpression { Value: null } ||
        expression is DefaultExpression && Nullable.GetUnderlyingType(expression.Type) is not null;

    private static bool IsKnownNotNull(Expression expression) =>
        expression is NewExpression { Constructor.DeclaringType.IsGenericType: true } &&
        expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
