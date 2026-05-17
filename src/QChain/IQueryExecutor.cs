using System.Linq.Expressions;

namespace QChain;

public interface IQueryExecutor<T>
{
    #region Any/All
    public Task<bool> AnyAsync(CancellationToken ct = default);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<bool> AllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Count/LongCount
    public Task<int> CountAsync(CancellationToken ct = default);
    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<long> LongCountAsync(CancellationToken ct = default);
    public Task<long> LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region ElementAt/ElementAtOrDefault
    public Task<T> ElementAtAsync(int index, CancellationToken ct = default);

    public Task<T?> ElementAtOrDefaultAsync(int index, CancellationToken ct = default);
    #endregion

    #region First/FirstOrDefault
    public Task<T> FirstAsync(CancellationToken ct = default);
    public Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<T?> FirstOrDefaultAsync(CancellationToken ct = default);
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Last/LastOrDefault
    public Task<T> LastAsync(CancellationToken ct = default);
    public Task<T> LastAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<T?> LastOrDefaultAsync(CancellationToken ct = default);
    public Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Single/SingleOrDefault
    public Task<T> SingleAsync(CancellationToken ct = default);
    public Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    public Task<T?> SingleOrDefaultAsync(CancellationToken ct = default);
    public Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    #endregion

    #region Min/Max
    public Task<T> MinAsync(CancellationToken ct = default);
    public Task<R> MinAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default);
    public Task<T> MaxAsync(CancellationToken ct = default);
    public Task<R> MaxAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default);
    #endregion

    #region Sum
    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken ct = default);
    public Task<decimal?> SumAsync(Expression<Func<T, decimal?>> selector, CancellationToken ct = default);

    public Task<int> SumAsync(Expression<Func<T, int>> selector, CancellationToken ct = default);
    public Task<int?> SumAsync(Expression<Func<T, int?>> selector, CancellationToken ct = default);

    public Task<long> SumAsync(Expression<Func<T, long>> selector, CancellationToken ct = default);
    public Task<long?> SumAsync(Expression<Func<T, long?>> selector, CancellationToken ct = default);

    public Task<float> SumAsync(Expression<Func<T, float>> selector, CancellationToken ct = default);
    public Task<float?> SumAsync(Expression<Func<T, float?>> selector, CancellationToken ct = default);

    public Task<double> SumAsync(Expression<Func<T, double>> selector, CancellationToken ct = default);
    public Task<double?> SumAsync(Expression<Func<T, double?>> selector, CancellationToken ct = default);
    #endregion

    #region Average
    public Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken ct = default);
    public Task<decimal?> AverageAsync(Expression<Func<T, decimal?>> selector, CancellationToken ct = default);

    public Task<float> AverageAsync(Expression<Func<T, float>> selector, CancellationToken ct = default);
    public Task<float?> AverageAsync(Expression<Func<T, float?>> selector, CancellationToken ct = default);

    public Task<double> AverageAsync(Expression<Func<T, double>> selector, CancellationToken ct = default);
    public Task<double?> AverageAsync(Expression<Func<T, double?>> selector, CancellationToken ct = default);
    #endregion

    #region ToList/Array
    public Task<T[]> ToArrayAsync(CancellationToken ct = default);
    public Task<List<T>> ToListAsync(CancellationToken ct = default);
    #endregion

    public string ToQueryString(IQuery<T> query);
}