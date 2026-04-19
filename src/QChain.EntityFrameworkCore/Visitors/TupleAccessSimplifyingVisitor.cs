using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class TupleAccessSimplifyingVisitor : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        var target = Visit(node.Expression);
        if (target is null)
            return node;

        if (ProjectionReduction.TryInlineMemberAccess(target, node.Member, out var rewritten))
            return Visit(rewritten);

        return node.Update(target);
    }
}
