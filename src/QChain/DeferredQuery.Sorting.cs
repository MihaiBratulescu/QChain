using System.Linq.Expressions;

namespace QChain;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> selector) =>
        new DeferredQuery<T, Q>(Source.OrderBy(Translate(selector)), Shape);

    public IOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> selector) =>
        new DeferredQuery<T, Q>(Source.OrderByDescending(Translate(selector)), Shape);

    public IOrderedQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> selector) =>
        new DeferredQuery<T, Q>((Source as IOrderedQueryable<Q>)!.ThenBy(Translate(selector)), Shape);

    public IOrderedQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> selector) =>
        new DeferredQuery<T, Q>((Source as IOrderedQueryable<Q>)!.ThenByDescending(Translate(selector)), Shape);

    public IQuery<T> Reverse() => new DeferredQuery<T, Q>(Source.Reverse(), Shape);
}
