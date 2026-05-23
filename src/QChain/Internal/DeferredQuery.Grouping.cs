using QChain.Visitors;
using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<(K Key, IEnumerable<T> Items)> GroupBy<K>(Expression<Func<T, K>> selector) =>
        new DeferredQuery<(K, IEnumerable<T>), IGrouping<K, Q>>(
            Source.GroupBy(Translate(selector)),
            g => new ValueTuple<K, IEnumerable<T>>(g.Key, g.AsQueryable().Select(Shape).AsEnumerable()));

    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, 
                                   Expression<Func<K, IEnumerable<T>, R>> resultsSelector) =>
        GroupBy(key, x => x, resultsSelector);


    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector) =>
       new DeferredQuery<R, R>(
           Source.GroupBy(Translate(key)).Select(TranslateGroup(selector)),
           x => x);

    public IQuery<R> GroupBy<K, E, R>(Expression<Func<T, K>> key, 
                                      Expression<Func<T, E>> elementSelector, 
                                      Expression<Func<K, IEnumerable<E>, R>> resultsSelector)
    {
        var group = Source.GroupBy(Translate(key), Translate(elementSelector));

        return new DeferredQuery<R, R>(group.Select(TranslateGroup(resultsSelector)), x => x);
    }

    #region Helpers

    private Expression<Func<IGrouping<G, Q>, R>> TranslateGroup<G, R>(Expression<Func<IGrouping<G, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<G, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<G, Q, T>(groupQ, selector.Parameters[0], Shape).Visit(selector.Body);
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<G, Q>, R>>(body, groupQ);
    }

    private static Expression<Func<IGrouping<K, E>, R>> TranslateGroup<K, E, R>(Expression<Func<K, IEnumerable<E>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, E>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, E>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], group).Visit(body)!;
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<K, E>, R>>(body, group);
    }
    #endregion
}
