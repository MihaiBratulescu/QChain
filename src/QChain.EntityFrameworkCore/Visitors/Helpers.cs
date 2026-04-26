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

internal sealed class GroupResultSelectorVisitor<K, Q, T> : ExpressionVisitor
{
    private readonly ParameterExpression _keyParam;
    private readonly Expression _keyReplacement;
    private readonly ParameterExpression _itemsParam;
    private readonly ParameterExpression _groupQ;
    private readonly Expression<Func<Q, T>> _shape;

    public GroupResultSelectorVisitor(
        ParameterExpression keyParam,
        Expression keyReplacement,
        ParameterExpression itemsParam,
        ParameterExpression groupQ,
        Expression<Func<Q, T>> shape)
    {
        _keyParam = keyParam;
        _keyReplacement = keyReplacement;
        _itemsParam = itemsParam;
        _groupQ = groupQ;
        _shape = shape;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _keyParam)
            return _keyReplacement;

        if (node == _itemsParam)
            return _groupQ;

        return base.VisitParameter(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // items.Count()
        if (IsCallOnItems(node))
        {
            var args = node.Arguments.ToArray();

            if (IsEnumerableMethod(node, nameof(Enumerable.Count), 1))
            {
                return Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Count),
                    new[] { typeof(Q) },
                    _groupQ);
            }

            if (IsEnumerableMethod(node, nameof(Enumerable.LongCount), 1))
            {
                return Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.LongCount),
                    new[] { typeof(Q) },
                    _groupQ);
            }

            // items.Sum(x => x.Amount), items.Any(x => ...), etc.
            // Translate lambda from T -> ... into Q -> ...
            if (args.Length >= 2 && args[0] == _itemsParam && args[1] is UnaryExpression { Operand: LambdaExpression lambda })
            {
                var translatedLambda = TranslateLambdaFromTToQ(lambda);
                var newArgs = args.ToArray();
                newArgs[0] = _groupQ;
                newArgs[1] = translatedLambda;

                var method = RewriteGenericMethod(node.Method);
                return Expression.Call(method, newArgs);
            }
        }

        return base.VisitMethodCall(node);
    }

    private bool IsCallOnItems(MethodCallExpression node)
    {
        return node.Arguments.Count > 0 && node.Arguments[0] == _itemsParam;
    }

    private static bool IsEnumerableMethod(MethodCallExpression node, string name, int argCount)
    {
        return node.Method.DeclaringType == typeof(Enumerable)
            && node.Method.Name == name
            && node.Arguments.Count == argCount;
    }

    private LambdaExpression TranslateLambdaFromTToQ(LambdaExpression lambda)
    {
        var qParam = Expression.Parameter(typeof(Q), lambda.Parameters[0].Name);

        var body = new ReplaceExpressionVisitor(
            lambda.Parameters[0],
            Expression.Invoke(_shape, qParam)
        ).Visit(lambda.Body)!;

        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda(body, qParam);
    }

    private static MethodInfo RewriteGenericMethod(MethodInfo method)
    {
        if (!method.IsGenericMethod)
            return method;

        var args = method.GetGenericArguments()
            .Select(t => t == typeof(T) ? typeof(Q) : t)
            .ToArray();

        return method.GetGenericMethodDefinition().MakeGenericMethod(args);
    }
}