using QChain.Internal.Helpers;
using QChain.Internal.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Builders;

internal static class GroupResultSelectorTranslator<T, Q>
{
    public static Expression<Func<IGrouping<K, Q>, R>> TranslateGroup<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<IGrouping<K, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<K, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<K, Q, T>(groupQ, selector.Parameters[0], query.Shape).Visit(selector.Body);
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, groupQ);
    }

    public static Expression<Func<IGrouping<K, E>, R>> TranslateElementGroup<K, E, R>(
        Expression<Func<K, IEnumerable<E>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, E>), "g");

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = Expression.Property(group, nameof(IGrouping<K, E>.Key)),
            [selector.Parameters[1]] = group
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, E>, R>>(body, group);
    }

    public static Expression<Func<IGrouping<K, Q>, R>> TranslateInternalElementGroup<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<K, IEnumerable<T>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, Q>), "g");

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = Expression.Property(group, nameof(IGrouping<K, Q>.Key)),
            [selector.Parameters[1]] = ComposeEnumerable(query.Shape, group)
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, group);
    }

    private static MethodCallExpression ComposeEnumerable<TRightInternal, TRightPublic>(
        Expression<Func<TRightInternal, TRightPublic>> itemShape,
        Expression enumerableExpression)
    {
        return Expression.Call(
            EnumerableSelectMethod.MakeGenericMethod(typeof(TRightInternal), typeof(TRightPublic)),
            enumerableExpression,
            itemShape);
    }

    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));
}
