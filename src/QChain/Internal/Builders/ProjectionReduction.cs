using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Builders;

internal static class ProjectionReduction
{
    public static bool TryInlineMemberAccess(Expression target, MemberInfo accessedMember, out Expression rewritten)
    {
        if (TryRewriteTupleAccess(target, accessedMember.Name, out rewritten))
            return true;

        if (TryRewriteObjectMemberAccess(target, accessedMember, out rewritten))
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

    private static bool TryRewriteObjectMemberAccess(Expression target, MemberInfo accessedMember, out Expression rewritten)
    {
        rewritten = null!;

        if (target is NewExpression { Members: not null } ne)
        {
            for (var i = 0; i < ne.Members.Count; i++)
            {
                if (ne.Members[i].Name != accessedMember.Name)
                    continue;

                rewritten = ne.Arguments[i];
                return true;
            }
        }

        if (target is MemberInitExpression mi)
        {
            foreach (var binding in mi.Bindings.OfType<MemberAssignment>())
            {
                if (binding.Member.Name != accessedMember.Name)
                    continue;

                rewritten = binding.Expression;
                return true;
            }
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
