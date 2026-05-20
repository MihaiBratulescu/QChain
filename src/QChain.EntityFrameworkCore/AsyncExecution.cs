using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace QChain;

public static class AsyncExecution
{
    extension<T>(IQuery<T> query)
    {
        #region Any/All
        public Task<bool> AnyAsync(CancellationToken ct = default) =>
            Query(query, q => q.AnyAsync(ct));
        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.AnyAsync(ct));
        public Task<bool> AllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, q => q.AllAsync(predicate, ct));
        #endregion

        #region Count/LongCount
        public Task<int> CountAsync(CancellationToken ct = default) =>
            Query(query, q => q.CountAsync(ct));
        public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.CountAsync(ct));
        public Task<long> LongCountAsync(CancellationToken ct = default) =>
            Query(query, q => q.LongCountAsync(ct));
        public Task<long> LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.LongCountAsync(ct));
        #endregion

        #region ElementAt/ElementAtOrDefault
        public Task<T> ElementAtAsync(int index, CancellationToken ct = default) =>
            query.AsQueryable().ElementAtAsync(index, ct);

        public Task<T?> ElementAtOrDefaultAsync(int index, CancellationToken ct = default) =>
            query.AsQueryable().ElementAtOrDefaultAsync(index, ct);
        #endregion

        #region First/FirstOrDefault
        public Task<T> FirstAsync(CancellationToken ct = default) =>
            Query(query, q => q.FirstAsync(ct));
        public Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.FirstAsync(ct));
        public Task<T?> FirstOrDefaultAsync(CancellationToken ct = default) =>
            Query(query, q => q.FirstOrDefaultAsync(ct));
        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.FirstOrDefaultAsync(ct));
        #endregion

        #region Last/LastOrDefault
        public Task<T> LastAsync(CancellationToken ct = default) =>
            Query(query, q => q.LastAsync(ct));
        public Task<T> LastAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.LastAsync(ct));
        public Task<T?> LastOrDefaultAsync(CancellationToken ct = default) =>
            Query(query, q => q.LastOrDefaultAsync(ct));
        public Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.LastOrDefaultAsync(ct));
        #endregion

        #region Single/SingleOrDefault
        public Task<T> SingleAsync(CancellationToken ct = default) =>
            Query(query, q => q.SingleAsync(ct));
        public Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.SingleAsync(ct));
        public Task<T?> SingleOrDefaultAsync(CancellationToken ct = default) =>
            Query(query, q => q.SingleOrDefaultAsync(ct));
        public Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Query(query, predicate, q => q.SingleOrDefaultAsync(ct));
        #endregion

        #region Min/Max
        public Task<T> MinAsync(CancellationToken ct = default) =>
            Query(query, q => q.MinAsync(ct));
        public Task<R> MinAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default) =>
            Query(query.Select(selector), q => q.MinAsync(ct));
        public Task<T> MaxAsync(CancellationToken ct = default) =>
            Query(query, q => q.MaxAsync(ct));
        public Task<R> MaxAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default) =>
            Query(query.Select(selector), q => q.MaxAsync(ct));
        #endregion

        #region Sum
        public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken ct = default) =>
            Query(query, q => q.SumAsync(selector, ct));
        public Task<decimal?> SumAsync(Expression<Func<T, decimal?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.SumAsync(selector, ct));

        public Task<int> SumAsync(Expression<Func<T, int>> selector, CancellationToken ct = default) =>
          Query(query, q => q.SumAsync(selector, ct));
        public Task<int?> SumAsync(Expression<Func<T, int?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.SumAsync(selector, ct));

        public Task<long> SumAsync(Expression<Func<T, long>> selector, CancellationToken ct = default) =>
          Query(query, q => q.SumAsync(selector, ct));
        public Task<long?> SumAsync(Expression<Func<T, long?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.SumAsync(selector, ct));

        public Task<float> SumAsync(Expression<Func<T, float>> selector, CancellationToken ct = default) =>
          Query(query, q => q.SumAsync(selector, ct));
        public Task<float?> SumAsync(Expression<Func<T, float?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.SumAsync(selector, ct));

        public Task<double> SumAsync(Expression<Func<T, double>> selector, CancellationToken ct = default) =>
          Query(query, q => q.SumAsync(selector, ct));
        public Task<double?> SumAsync(Expression<Func<T, double?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.SumAsync(selector, ct));
        #endregion

        #region Average
        public Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken ct = default) =>
            Query(query, q => q.AverageAsync(selector, ct));
        public Task<decimal?> AverageAsync(Expression<Func<T, decimal?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.AverageAsync(selector, ct));

        public Task<float> AverageAsync(Expression<Func<T, float>> selector, CancellationToken ct = default) =>
          Query(query, q => q.AverageAsync(selector, ct));
        public Task<float?> AverageAsync(Expression<Func<T, float?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.AverageAsync(selector, ct));

        public Task<double> AverageAsync(Expression<Func<T, double>> selector, CancellationToken ct = default) =>
          Query(query, q => q.AverageAsync(selector, ct));
        public Task<double?> AverageAsync(Expression<Func<T, double?>> selector, CancellationToken ct = default) =>
            Query(query, q => q.AverageAsync(selector, ct));
        #endregion

        #region ToList/Array
        public Task<T[]> ToArrayAsync(CancellationToken ct = default) => Query(query, q => q.ToArrayAsync(ct));
        public Task<List<T>> ToListAsync(CancellationToken ct = default) => Query(query, q => q.ToListAsync(ct));
        #endregion

        public Task<bool> ContainsAsync(T item, CancellationToken ct = default) =>
            Query(query, q => q.ContainsAsync(item, ct));

        #region Helpers
        internal Task<R> Query<R>(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, Task<R>> executor) =>
            executor(query.Where(predicate).AsQueryable());
        internal Task<R> Query<R>(Func<IQueryable<T>, Task<R>> executor) =>
            executor(query.AsQueryable());
        #endregion
    }
}
