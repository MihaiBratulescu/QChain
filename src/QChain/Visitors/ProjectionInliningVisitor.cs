using System.Linq.Expressions;

namespace QChain.Visitors;

internal sealed class ProjectionInliningVisitor(ParameterExpression from, Expression to) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == from ? to : base.VisitParameter(node);

    protected override Expression VisitMember(MemberExpression node)
    {
        var target = Visit(node.Expression);

        if (target is null)
            return base.VisitMember(node);

        if (ProjectionReduction.TryInlineMemberAccess(UnwrapSafeConversion(target), node.Member, out var rewritten))
            return Visit(rewritten);

        return node.Update(target);
    }

    private static Expression UnwrapSafeConversion(Expression expression)
    {
        if (expression is not UnaryExpression unary ||
            unary.NodeType is not (ExpressionType.Convert or ExpressionType.ConvertChecked) ||
            !expression.Type.IsAssignableFrom(unary.Operand.Type))
        {
            return expression;
        }

        return unary.Operand;
    }
}
