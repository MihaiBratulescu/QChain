using QChain.Predicates;
using System.Linq.Expressions;

namespace QChain;

public interface IQuery<T>
{
    IQueryable<T> AsQueryable();

    IQuery<T?> DefaultIfEmpty();
    
    IQuery<T> Distinct();

    #region Filtering
    IQuery<T> Where(Func<T, Predicate> predicate);
    IQuery<T> Where(Expression<Func<T, bool>> predicate);
    #endregion

    #region Grouping
    IQuery<IGrouping<K, T>> GroupBy<K>(Expression<Func<T, K>> key);
    IQuery<IGrouping<K, E>> GroupBy<K, E>(Expression<Func<T, K>> keySelector, Expression<Func<T, E>> elementSelector);

    IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector);
    IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> keySelector, Expression<Func<K, IEnumerable<T>, R>> resultsSelector);
    IQuery<R> GroupBy<K, E, R>(Expression<Func<T, K>> keySelector, Expression<Func<T, E>> elementSelector, Expression<Func<K, IEnumerable<E>, R>> resultsSelector);
    #endregion

    #region Joins
    IQuery<(T, R)> Join<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey);
    IQuery<TOut> Join<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result);

    IQuery<(T, IEnumerable<R>)> GroupJoin<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey);
    IQuery<TOut> GroupJoin<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result);

#if NET10_0_OR_GREATER
    IQuery<(T, R?)> LeftJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey);
    IQuery<TResult> LeftJoin<R, K, TResult>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T, R?, TResult>> resultSelector);

    IQuery<(T?, R)> RightJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey);
    IQuery<TResult> RightJoin<R, K, TResult>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T?, R, TResult>> resultSelector);
#endif

    #endregion

    #region Paging
    IQuery<T> Skip(int count);
    IQuery<T> Take(int count);
    IQuery<T> Page(int index, int count);
    #endregion

    #region Projection
    IQuery<R> Select<R>(Expression<Func<T, R>> mapping);
    IQuery<R> SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector);
    IQuery<R> SelectMany<C, R>(Expression<Func<T, IEnumerable<C>>> collectionSelector,
                               Expression<Func<T, C, R>> resultSelector);
    #endregion

    #region Sets
    IQuery<T> Union(IQuery<T> other);
    
    IQuery<T> Concat(IQuery<T> other); 
    
    IQuery<T> Except(IQuery<T> other); 
    IQuery<T> ExceptBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector); 
    
    IQuery<T> Intersect(IQuery<T> other);
    IQuery<T> IntersectBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector);
    #endregion

    #region Sorting
    IOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> selector);
    IOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> selector);
    IQuery<T> Reverse();
    #endregion

    //#region Caching
    //ICachedQuery<T> WithCaching(string key, TimeSpan expiry);
    //#endregion
}
