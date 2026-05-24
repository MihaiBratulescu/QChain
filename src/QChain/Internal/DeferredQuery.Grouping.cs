using QChain.Visitors;
using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<(K Key, IEnumerable<T> Items)> GroupBy<K>(Expression<Func<T, K>> selector) =>
        new DeferredQuery<(K, IEnumerable<T>), IGrouping<K, Q>>(
            Source.GroupBy(Translate(selector)),
            g => new ValueTuple<K, IEnumerable<T>>(g.Key, g.AsQueryable().Select(Shape).AsEnumerable()));

    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector) =>
       new DeferredQuery<R, R>(Source.GroupBy(Translate(key)).Select(TranslateGroup(selector)), x => x);

    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<K, IEnumerable<T>, R>> resultsSelector) =>
        GroupBy(key, x => x, resultsSelector);

    public IQuery<R> GroupBy<K, E, R>(Expression<Func<T, K>> key, Expression<Func<T, E>> elementSelector, Expression<Func<K, IEnumerable<E>, R>> resultsSelector) =>
        new DeferredQuery<R, R>(Source.GroupBy(Translate(key), Translate(elementSelector)).Select(TranslateElementGroup(resultsSelector)), x => x);

    #region Helpers

    private Expression<Func<IGrouping<G, Q>, R>> TranslateGroup<G, R>(Expression<Func<IGrouping<G, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<G, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<G, Q, T>(groupQ, selector.Parameters[0], Shape).Visit(selector.Body);
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<G, Q>, R>>(body, groupQ);
    }

    private static Expression<Func<IGrouping<K, E>, R>> TranslateElementGroup<K, E, R>(Expression<Func<K, IEnumerable<E>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, E>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, E>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], group).Visit(body)!;
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<K, E>, R>>(body, group);
    }

    private static Expression<Func<IGrouping<K, Q>, Pair<R1, R2>>> BuildTuplePairProjection<K, R1, R2>(
        Expression<Func<IGrouping<K, Q>, ValueTuple<R1, R2>>> selector)
    {
        var body = selector.Body;

        if (!ProjectionReduction.TryRewriteTupleAccess(body, nameof(ValueTuple<R1, R2>.Item1), out var left) ||
            !ProjectionReduction.TryRewriteTupleAccess(body, nameof(ValueTuple<R1, R2>.Item2), out var right))
        {
            throw new NotSupportedException("Unsupported ValueTuple group projection.");
        }

        return Expression.Lambda<Func<IGrouping<K, Q>, Pair<R1, R2>>>(
            Expression.MemberInit(
                Expression.New(typeof(Pair<R1, R2>)),
                Expression.Bind(typeof(Pair<R1, R2>).GetProperty(nameof(Pair<R1, R2>.Left))!, left),
                Expression.Bind(typeof(Pair<R1, R2>).GetProperty(nameof(Pair<R1, R2>.Right))!, right)),
            selector.Parameters);
    }

    private static Expression<Func<IGrouping<K, Q>, Pair<Pair<K1, K2>, R2>>> BuildNestedTuplePairProjection<K, K1, K2, R2>(
        Expression<Func<IGrouping<K, Q>, ValueTuple<ValueTuple<K1, K2>, R2>>> selector)
    {
        var body = selector.Body;

        if (!ProjectionReduction.TryRewriteTupleAccess(body, nameof(ValueTuple<ValueTuple<K1, K2>, R2>.Item1), out var leftTuple) ||
            !ProjectionReduction.TryRewriteTupleAccess(body, nameof(ValueTuple<ValueTuple<K1, K2>, R2>.Item2), out var right) ||
            !ProjectionReduction.TryRewriteTupleAccess(leftTuple, nameof(ValueTuple<K1, K2>.Item1), out var left1) ||
            !ProjectionReduction.TryRewriteTupleAccess(leftTuple, nameof(ValueTuple<K1, K2>.Item2), out var left2))
        {
            throw new NotSupportedException("Unsupported nested ValueTuple group projection.");
        }

        return Expression.Lambda<Func<IGrouping<K, Q>, Pair<Pair<K1, K2>, R2>>>(
            Expression.MemberInit(
                Expression.New(typeof(Pair<Pair<K1, K2>, R2>)),
                Expression.Bind(
                    typeof(Pair<Pair<K1, K2>, R2>).GetProperty(nameof(Pair<Pair<K1, K2>, R2>.Left))!,
                    Expression.MemberInit(
                        Expression.New(typeof(Pair<K1, K2>)),
                        Expression.Bind(typeof(Pair<K1, K2>).GetProperty(nameof(Pair<K1, K2>.Left))!, left1),
                        Expression.Bind(typeof(Pair<K1, K2>).GetProperty(nameof(Pair<K1, K2>.Right))!, left2))),
                Expression.Bind(typeof(Pair<Pair<K1, K2>, R2>).GetProperty(nameof(Pair<Pair<K1, K2>, R2>.Right))!, right)),
            selector.Parameters);
    }
    #endregion
}
