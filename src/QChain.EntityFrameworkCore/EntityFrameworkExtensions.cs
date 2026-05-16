using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace QChain;

public static class EntityFrameworkExtensions
{
    #region Any/All
    public static Task<bool> AnyAsync<T>(this IQuery<T> query, CancellationToken ct = default) => 
        Query(query, q => q.AnyAsync(ct));
    public static Task<bool> AnyAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => 
        Query(query, predicate, q => q.AnyAsync(ct));
    public static Task<bool> AllAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => 
        Query(query, q => q.AllAsync(predicate, ct));
    #endregion

    #region Count/LongCount
    public static Task<int> CountAsync<T>(this IQuery<T> query, CancellationToken ct = default) => 
        Query(query, q => q.CountAsync(ct));
    public static Task<int> CountAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => 
        Query(query, predicate, q => q.CountAsync(ct));
    public static Task<long> LongCountAsync<T>(this IQuery<T> query, CancellationToken ct = default) =>
        Query(query, q => q.LongCountAsync(ct));
    public static Task<long> LongCountAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Query(query, predicate, q => q.LongCountAsync(ct));
    #endregion

    #region ElementAt/ElementAtOrDefault
    public static Task<T> ElementAtAsync<T>(this IQuery<T> query, int index, CancellationToken ct = default) =>
        query.AsQueryable().ElementAtAsync(index, ct);

    public static Task<T?> ElementAtOrDefaultAsync<T>(this IQuery<T> query, int index, CancellationToken ct = default) =>
        query.AsQueryable().ElementAtOrDefaultAsync(index, ct);
    #endregion

    #region First/FirstOrDefault
    public static Task<T> FirstAsync<T>(this IQuery<T> query, CancellationToken ct = default) => 
        Query(query, q => q.FirstAsync(ct));
    public static Task<T> FirstAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => 
        Query(query, predicate, q => q.FirstAsync(ct));
    public static Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query, CancellationToken ct = default) =>
        Query(query, q => q.FirstOrDefaultAsync(ct));
    public static Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Query(query, predicate, q => q.FirstOrDefaultAsync(ct));
    #endregion

    #region Last/LastOrDefault
    public static Task<T> LastAsync<T>(this IQuery<T> query, CancellationToken ct = default) =>
        Query(query, q => q.LastAsync(ct));
    public static Task<T> LastAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Query(query, predicate, q => q.LastAsync(ct));
    public static Task<T?> LastOrDefaultAsync<T>(this IQuery<T> query, CancellationToken ct = default) =>
        Query(query, q => q.LastOrDefaultAsync(ct));
    public static Task<T?> LastOrDefaultAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Query(query, predicate, q => q.LastOrDefaultAsync(ct));
    #endregion

    #region Single/SingleOrDefault
    public static Task<T> SingleAsync<T>(this IQuery<T> query, CancellationToken ct = default) =>
        Query(query, q => q.SingleAsync(ct));
    public static Task<T> SingleAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Query(query, predicate, q => q.SingleAsync(ct));
    public static Task<T?> SingleOrDefaultAsync<T>(this IQuery<T> query, CancellationToken ct = default) => 
        Query(query, q => q.SingleOrDefaultAsync(ct));
    public static Task<T?> SingleOrDefaultAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => 
        Query(query, predicate, q => q.SingleOrDefaultAsync(ct));
    #endregion

    #region Min/Max
    public static Task<T> MinAsync<T>(this IQuery<T> query, CancellationToken ct = default) =>
        Query(query, q => q.MinAsync(ct));
    public static Task<R> MinAsync<T, R>(this IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default) =>
        Query(query.Map(selector), q => q.MinAsync(ct));
    public static Task<T> MaxAsync<T>(this IQuery<T> query, CancellationToken ct = default) =>
        Query(query, q => q.MaxAsync(ct));
    public static Task<R> MaxAsync<T, R>(this IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default) =>
        Query(query.Map(selector), q => q.MaxAsync(ct));
    #endregion

    #region Sum
    public static Task<decimal> SumAsync<T>(this IQuery<T> query, Expression<Func<T, decimal>> selector, CancellationToken ct = default) => 
        Query(query, q => q.SumAsync(selector, ct));
    public static Task<decimal?> SumAsync<T>(this IQuery<T> query, Expression<Func<T, decimal?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.SumAsync(selector, ct));

    public static Task<int> SumAsync<T>(this IQuery<T> query, Expression<Func<T, int>> selector, CancellationToken ct = default) =>
      Query(query, q => q.SumAsync(selector, ct));
    public static Task<int?> SumAsync<T>(this IQuery<T> query, Expression<Func<T, int?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.SumAsync(selector, ct));

    public static Task<long> SumAsync<T>(this IQuery<T> query, Expression<Func<T, long>> selector, CancellationToken ct = default) =>
      Query(query, q => q.SumAsync(selector, ct));
    public static Task<long?> SumAsync<T>(this IQuery<T> query, Expression<Func<T, long?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.SumAsync(selector, ct));

    public static Task<float> SumAsync<T>(this IQuery<T> query, Expression<Func<T, float>> selector, CancellationToken ct = default) =>
      Query(query, q => q.SumAsync(selector, ct));
    public static Task<float?> SumAsync<T>(this IQuery<T> query, Expression<Func<T, float?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.SumAsync(selector, ct));

    public static Task<double> SumAsync<T>(this IQuery<T> query, Expression<Func<T, double>> selector, CancellationToken ct = default) =>
      Query(query, q => q.SumAsync(selector, ct));
    public static Task<double?> SumAsync<T>(this IQuery<T> query, Expression<Func<T, double?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.SumAsync(selector, ct));
    #endregion

    #region Average
    public static Task<decimal> AverageAsync<T>(this IQuery<T> query, Expression<Func<T, decimal>> selector, CancellationToken ct = default) =>
        Query(query, q => q.AverageAsync(selector, ct));
    public static Task<decimal?> AverageAsync<T>(this IQuery<T> query, Expression<Func<T, decimal?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.AverageAsync(selector, ct));
    
    public static Task<float> AverageAsync<T>(this IQuery<T> query, Expression<Func<T, float>> selector, CancellationToken ct = default) =>
      Query(query, q => q.AverageAsync(selector, ct));
    public static Task<float?> AverageAsync<T>(this IQuery<T> query, Expression<Func<T, float?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.AverageAsync(selector, ct));

    public static Task<double> AverageAsync<T>(this IQuery<T> query, Expression<Func<T, double>> selector, CancellationToken ct = default) =>
      Query(query, q => q.AverageAsync(selector, ct));
    public static Task<double?> AverageAsync<T>(this IQuery<T> query, Expression<Func<T, double?>> selector, CancellationToken ct = default) =>
        Query(query, q => q.AverageAsync(selector, ct));
    #endregion

    #region ToList/Array
    public static Task<T[]> ToArrayAsync<T>(this IQuery<T> query, CancellationToken ct = default) => Query(query, q => q.ToArrayAsync(ct));
    public static Task<List<T>> ToListAsync<T>(this IQuery<T> query, CancellationToken ct = default) => Query(query, q => q.ToListAsync(ct));
    #endregion

    #region Single/Split
    public static IQuery<T> AsSingleQuery<T>(this IQuery<T> query) where T : class =>
        new DeferredQuery<T, T>(query.AsQueryable().AsSingleQuery(), q => q);

    public static IQuery<T> AsSplitQuery<T>(this IQuery<T> query) where T : class =>
        new DeferredQuery<T, T>(query.AsQueryable().AsSplitQuery(), q => q);
    #endregion

    #region Tracking
    public static IQuery<T> AsNoTracking<T>(this IQuery<T> query) where T : class =>
        new DeferredQuery<T, T>(query.AsQueryable().AsNoTracking(), q => q);

    public static IQuery<T> AsNoTrackingWithIdentityResolution<T>(this IQuery<T> query) where T : class =>
        new DeferredQuery<T, T>(query.AsQueryable().AsNoTrackingWithIdentityResolution(), q => q);

    public static IQuery<T> AsTracking<T>(this IQuery<T> query) where T : class =>
        new DeferredQuery<T, T>(query.AsQueryable().AsTracking(), q => q);
    #endregion

    public static IQuery<T> Include<T, E>(this IQuery<T> query, Expression<Func<T, E>> include) where T : class =>
        new DeferredQuery<T, T>(query.AsQueryable().Include(include), q => q);

    public static string ToQueryString<T>(this IQuery<T> query) =>
        query.AsQueryable().ToQueryString();

    public static Task<bool> ContainsAsync<T>(this IQuery<T> query, T item, CancellationToken ct = default) =>
        Query(query, q => q.ContainsAsync(item, ct));

    #region Helpers
    internal static Task<R> Query<T, R>(this IQuery<T> query, Expression<Func<T, bool>> predicate, Func<IQueryable<T>, Task<R>> executor) =>
        executor(query.Where(predicate).AsQueryable());
    internal static Task<R> Query<T, R>(this IQuery<T> query, Func<IQueryable<T>, Task<R>> executor) =>
        executor(query.AsQueryable()); 
    #endregion
}