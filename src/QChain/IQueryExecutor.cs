using System.Linq.Expressions;

namespace QChain;

public interface IQueryExecutor<T>
{
    #region Any/All
    public Task<bool> AnyAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<bool> AnyAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<bool> AllAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Count/LongCount
    public Task<int> CountAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<int> CountAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<long> LongCountAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<long> LongCountAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region ElementAt/ElementAtOrDefault
    public Task<T> ElementAtAsync(IQuery<T> query, int index, CancellationToken ct = default);

    public Task<T?> ElementAtOrDefaultAsync(IQuery<T> query, int index, CancellationToken ct = default);
    #endregion

    #region First/FirstOrDefault
    public Task<T> FirstAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<T> FirstAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<T?> FirstOrDefaultAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<T?> FirstOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Last/LastOrDefault
    public Task<T> LastAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<T> LastAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<T?> LastOrDefaultAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<T?> LastOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Single/SingleOrDefault
    public Task<T> SingleAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<T> SingleAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<T?> SingleOrDefaultAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<T?> SingleOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Min/Max
    public Task<T> MinAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<R> MinAsync<R>(IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default);
    public Task<T> MaxAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<R> MaxAsync<R>(IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default);
    #endregion

    #region Sum
    public Task<decimal> SumAsync(IQuery<T> query, Expression<Func<T, decimal>> selector, CancellationToken ct = default);
    public Task<decimal?> SumAsync(IQuery<T> query, Expression<Func<T, decimal?>> selector, CancellationToken ct = default);

    public Task<int> SumAsync(IQuery<T> query, Expression<Func<T, int>> selector, CancellationToken ct = default);
    public Task<int?> SumAsync(IQuery<T> query, Expression<Func<T, int?>> selector, CancellationToken ct = default);

    public Task<long> SumAsync(IQuery<T> query, Expression<Func<T, long>> selector, CancellationToken ct = default);
    public Task<long?> SumAsync(IQuery<T> query, Expression<Func<T, long?>> selector, CancellationToken ct = default);

    public Task<float> SumAsync(IQuery<T> query, Expression<Func<T, float>> selector, CancellationToken ct = default);
    public Task<float?> SumAsync(IQuery<T> query, Expression<Func<T, float?>> selector, CancellationToken ct = default);

    public Task<double> SumAsync(IQuery<T> query, Expression<Func<T, double>> selector, CancellationToken ct = default);
    public Task<double?> SumAsync(IQuery<T> query, Expression<Func<T, double?>> selector, CancellationToken ct = default);
    #endregion

    #region Average
    public Task<decimal> AverageAsync(IQuery<T> query, Expression<Func<T, decimal>> selector, CancellationToken ct = default);
    public Task<decimal?> AverageAsync(IQuery<T> query, Expression<Func<T, decimal?>> selector, CancellationToken ct = default);

    public Task<float> AverageAsync(IQuery<T> query, Expression<Func<T, float>> selector, CancellationToken ct = default);
    public Task<float?> AverageAsync(IQuery<T> query, Expression<Func<T, float?>> selector, CancellationToken ct = default);

    public Task<double> AverageAsync(IQuery<T> query, Expression<Func<T, double>> selector, CancellationToken ct = default);
    public Task<double?> AverageAsync(IQuery<T> query, Expression<Func<T, double?>> selector, CancellationToken ct = default);
    #endregion

    #region ToList/Array
    public Task<T[]> ToArrayAsync(IQuery<T> query, CancellationToken ct = default);
    public Task<List<T>> ToListAsync(IQuery<T> query, CancellationToken ct = default);
    #endregion

    public string ToQueryString(IQuery<T> query);
}