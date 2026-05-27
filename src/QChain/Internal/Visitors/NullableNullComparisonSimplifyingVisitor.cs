using System.Linq.Expressions;

namespace QChain.Internal.Visitors;

internal sealed class NullableNullComparisonSimplifyingVisitor : ExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType is not (ExpressionType.Equal or ExpressionType.NotEqual))
            return base.VisitBinary(node);

        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (TryNullComparison(left, right, node.NodeType, out var rewritten) ||
            TryNullComparison(right, left, node.NodeType, out rewritten))
        {
            return rewritten;
        }

        return node.Update(left, node.Conversion, right);
    }

    private static bool TryNullComparison(
        Expression nullable,
        Expression other,
        ExpressionType comparison,
        out Expression rewritten)
    {
        rewritten = null!;

        if (!IsNullableNull(other) ||
            Nullable.GetUnderlyingType(nullable.Type) is null)
        {
            return false;
        }

        if (!TryHasValue(nullable, out var hasValue))
            return false;

        rewritten = comparison == ExpressionType.Equal
            ? Expression.Not(hasValue)
            : hasValue;

        return true;
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

    private static bool IsNullableNull(Expression expression) =>
        expression is ConstantExpression { Value: null } ||
        expression is DefaultExpression && Nullable.GetUnderlyingType(expression.Type) is not null;

    private static bool IsKnownNotNull(Expression expression) =>
        expression is NewExpression { Constructor.DeclaringType.IsGenericType: true } &&
        expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
