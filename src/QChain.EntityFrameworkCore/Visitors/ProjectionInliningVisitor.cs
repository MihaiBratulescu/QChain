using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class ProjectionInliningVisitor(ParameterExpression from, Expression to) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == from ? to : base.VisitParameter(node);

    protected override Expression VisitMember(MemberExpression node)
    {
        var target = Visit(node.Expression);

        if (target is null)
            return base.VisitMember(node);

        if (ProjectionReduction.TryInlineMemberAccess(target, node.Member, out var rewritten))
            return Visit(rewritten);

        return node.Update(target);
    }
}
