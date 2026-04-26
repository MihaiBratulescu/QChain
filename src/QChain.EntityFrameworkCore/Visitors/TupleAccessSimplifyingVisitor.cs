using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class TupleAccessSimplifyingVisitor : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        var expr = Helpers.StripConvert(Visit(node.Expression));

        if (expr is MethodCallExpression call &&
            Helpers.IsValueTupleCreate(call.Method) &&
            Helpers.TryGetTupleIndex(node.Member.Name, out var index))
        {
            return call.Arguments[index];
        }

        if (expr is NewExpression ne &&
            ne.Type.FullName!.StartsWith("System.ValueTuple`") &&
            Helpers.TryGetTupleIndex(node.Member.Name, out var index2))
        {
            return ne.Arguments[index2];
        }

        return node.Update(expr);
    }
}
