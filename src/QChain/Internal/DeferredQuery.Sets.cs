using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Union(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Union((other as DeferredQuery<T, Q>)!.Source), Shape);
    
    public IQuery<T> Concat(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Concat((other as DeferredQuery<T, Q>)!.Source), Shape);

    public IQuery<T> Except(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Except((other as DeferredQuery<T, Q>)!.Source), Shape);
    public IQuery<T> ExceptBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector)
    {
        var translated = Translate(keySelector);
        var parameter = translated.Parameters[0];

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(K)],
            Expression.Constant(keys),
            translated.Body);

        var predicate = Expression.Lambda<Func<Q, bool>>(
            Expression.Not(contains), parameter);

        return new DeferredQuery<T, Q>(Source.Where(predicate), Shape);
    }

    public IQuery<T> Intersect(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Intersect((other as DeferredQuery<T, Q>)!.Source), Shape);
    public IQuery<T> IntersectBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector)
    {
        var translated = Translate(keySelector);

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(K)],
            Expression.Constant(keys),
            translated.Body);

        var predicate = Expression.Lambda<Func<Q, bool>>(
            contains, translated.Parameters);

        return new DeferredQuery<T, Q>(Source.Where(predicate), Shape);
    }
}