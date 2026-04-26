using System.Linq.Expressions;

namespace QChain;

public interface IQuery<T>
{
    IQueryable<T> AsQueryable();

    #region Filtering
    IQuery<T> Where(Expression<Func<T, bool>> predicate);
    IQuery<T> Distinct();
    IQuery<R> DistinctBy<R>(Expression<Func<T, R>> selector);
    #endregion

    #region Grouping
    IQuery<(K Key, IEnumerable<T> Items)> GroupBy<K>(Expression<Func<T, K>> key);
    IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector);
    #endregion

    #region Projection
    IQuery<R> Map<R>(Expression<Func<T, R>> mapping);
    IQuery<R> Flatten<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector);
    #endregion

    #region Sorting
    IOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> selector);
    IOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> selector);
    #endregion

    #region Joins
    IQuery<(T, R)> Join<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey);
    IQuery<TOut> Join<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result);

    IQuery<(T, IEnumerable<R>)> GroupJoin<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey);
    IQuery<TOut> GroupJoin<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result);
    #endregion

    #region Caching
    ICachedQuery<T> WithCaching(string key, TimeSpan expiry);
    #endregion
}
