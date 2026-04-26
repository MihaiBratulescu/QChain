using System.Linq.Expressions;
using System.Reflection;

namespace QChain.EntityFrameworkCore.Visitors;

internal static class Helpers
{
    public static bool TryInlineMemberAccess(Expression target, MemberInfo accessedMember, out Expression rewritten)
    {
        target = StripConvert(target);

        if (TryRewriteTupleAccess(target, accessedMember.Name, out rewritten))
            return true;

        if (target is NewExpression e && e.Members is not null)
        {
            for (var i = 0; i < e.Members.Count; i++)
            {
                if (SameMember(e.Members[i], accessedMember))
                {
                    rewritten = e.Arguments[i];
                    return true;
                }
            }
        }

        if (target is MemberInitExpression mie)
        {
            var binding = mie.Bindings
                .OfType<MemberAssignment>()
                .FirstOrDefault(x => SameMember(x.Member, accessedMember));

            if (binding is not null)
            {
                rewritten = binding.Expression;
                return true;
            }
        }

        rewritten = null!;
        return false;
    }

    public static bool TryRewriteTupleAccess(Expression tupleExpression, string memberName, out Expression rewritten)
    {
        rewritten = null!;
        tupleExpression = StripConvert(tupleExpression);

        if (!TryGetTupleIndex(memberName, out var index))
            return false;

        if (tupleExpression is MethodCallExpression mc &&
            IsValueTupleCreate(mc.Method) &&
            index < mc.Arguments.Count)
        {
            rewritten = mc.Arguments[index];
            return true;
        }

        if (tupleExpression is NewExpression ne &&
            IsValueTupleType(ne.Type) &&
            index < ne.Arguments.Count)
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

    public static bool IsValueTupleType(Type type) =>
        type.IsValueType &&
        type.IsGenericType &&
        type.FullName is not null &&
        type.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal);

    public static bool IsValueTupleCreate(MethodInfo method) =>
        method.DeclaringType == typeof(ValueTuple) &&
        method.Name == nameof(ValueTuple.Create);

    public static bool SameMember(MemberInfo left, MemberInfo right) =>
        left == right || (left.MetadataToken == right.MetadataToken && left.Module == right.Module);

    public static Expression StripConvert(Expression e)
    {
        while (e is UnaryExpression u &&
               (u.NodeType == ExpressionType.Convert ||
                u.NodeType == ExpressionType.ConvertChecked))
        {
            e = u.Operand;
        }
        return e;
    }
}
