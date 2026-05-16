using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Visitors;

internal static class ProjectionReduction
{
    public static bool TryInlineMemberAccess(Expression target, MemberInfo accessedMember, out Expression rewritten)
    {
        if (TryRewriteTupleAccess(target, accessedMember.Name, out rewritten))
            return true;

        rewritten = null!;
        return false;
    }

    public static bool TryRewriteTupleAccess(Expression tupleExpression, string memberName, out Expression rewritten)
    {
        rewritten = null!;

        if (!TryGetTupleIndex(memberName, out var index))
            return false;

        if (tupleExpression is MethodCallExpression mc)
        {
            rewritten = mc.Arguments[index];
            return true;
        }

        if (tupleExpression is NewExpression ne)
        {
            rewritten = ne.Arguments[index];
            return true;
        }

        return false;
    }

    public static bool TryGetTupleIndex(string memberName, out int index)
    {
        index = -1;

        if (!memberName.StartsWith("Item", StringComparison.Ordinal))
            return false;

        if (!int.TryParse(memberName.AsSpan(4), out var n) || n <= 0)
            return false;

        index = n - 1;
        return true;
    }
}
