using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Helpers;

internal static class ProjectionReduction
{
    public static bool TryInlineMemberAccess(Expression target, MemberInfo member, out Expression value)
    {
        if (target is NewExpression { Members: not null } projection)
        {
            for (var index = 0; index < projection.Members.Count; index++)
            {
                if (projection.Members[index].Name == member.Name)
                {
                    value = projection.Arguments[index];
                    return true;
                }
            }
        }

        if (target is MemberInitExpression initialized)
        {
            var assignment = initialized.Bindings.OfType<MemberAssignment>()
                .FirstOrDefault(binding => binding.Member.Name == member.Name);
            if (assignment is not null)
            {
                value = assignment.Expression;
                return true;
            }
        }

        value = null!;
        return false;
    }
}
