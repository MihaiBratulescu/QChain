using QChain.Internal;
using System.Linq.Expressions;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> selector) =>
        new Query<T, Q>(Source.OrderBy(Translate(selector)), Shape);

    public IOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> selector) =>
        new Query<T, Q>(Source.OrderByDescending(Translate(selector)), Shape);

    public IOrderedQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> selector) =>
        new Query<T, Q>((Source as IOrderedQueryable<Q>)!.ThenBy(Translate(selector)), Shape);

    public IOrderedQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> selector) =>
        new Query<T, Q>((Source as IOrderedQueryable<Q>)!.ThenByDescending(Translate(selector)), Shape);

    public IQuery<T> Reverse() => new Query<T, Q>(Source.Reverse(), Shape);
}
