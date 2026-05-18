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
        new DeferredQuery<R, R>(
            Source.GroupBy(Translate(key)).Select(TranslateGroup(selector)),
            x => x);

    private Expression<Func<IGrouping<G, Q>, R>> TranslateGroup<G, R>(Expression<Func<IGrouping<G, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<G, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<G, Q, T>(groupQ, selector.Parameters[0], Shape).Visit(selector.Body);
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<G, Q>, R>>(body, groupQ);
    }
}
