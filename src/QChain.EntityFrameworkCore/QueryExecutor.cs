

using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

internal class QueryExecutor<T> : IQueryExecutor<T>
{
    public Task<bool> AllAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AnyAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AnyAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<decimal> AverageAsync(IQuery<T> query, Expression<Func<T, decimal>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<decimal?> AverageAsync(IQuery<T> query, Expression<Func<T, decimal?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<float> AverageAsync(IQuery<T> query, Expression<Func<T, float>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<float?> AverageAsync(IQuery<T> query, Expression<Func<T, float?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<double> AverageAsync(IQuery<T> query, Expression<Func<T, double>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<double?> AverageAsync(IQuery<T> query, Expression<Func<T, double?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> ElementAtAsync(IQuery<T> query, int index, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T?> ElementAtOrDefaultAsync(IQuery<T> query, int index, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> FirstAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> FirstAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T?> FirstOrDefaultAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T?> FirstOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> LastAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> LastAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T?> LastOrDefaultAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T?> LastOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> LongCountAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> LongCountAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> MaxAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<R> MaxAsync<R>(IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> MinAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<R> MinAsync<R>(IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> SingleAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> SingleAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T?> SingleOrDefaultAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T?> SingleOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<decimal> SumAsync(IQuery<T> query, Expression<Func<T, decimal>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<decimal?> SumAsync(IQuery<T> query, Expression<Func<T, decimal?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> SumAsync(IQuery<T> query, Expression<Func<T, int>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int?> SumAsync(IQuery<T> query, Expression<Func<T, int?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> SumAsync(IQuery<T> query, Expression<Func<T, long>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<long?> SumAsync(IQuery<T> query, Expression<Func<T, long?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<float> SumAsync(IQuery<T> query, Expression<Func<T, float>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<float?> SumAsync(IQuery<T> query, Expression<Func<T, float?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<double> SumAsync(IQuery<T> query, Expression<Func<T, double>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<double?> SumAsync(IQuery<T> query, Expression<Func<T, double?>> selector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<T[]> ToArrayAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> ToListAsync(IQuery<T> query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public string ToQueryString(IQuery<T> query)
    {
        throw new NotImplementedException();
    }
}
