using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

public class QueryExecutor<T>(IQuery<T> query) : IQueryExecutor<T>
{
    #region Async
    public Task<bool> AllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.AllAsync(predicate, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => query.AnyAsync(ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.AnyAsync(predicate, ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => query.CountAsync(ct);

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.CountAsync(predicate, ct);

    public Task<long> LongCountAsync(CancellationToken ct = default)
        => query.LongCountAsync(ct);

    public Task<long> LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.LongCountAsync(predicate, ct);

    public Task<T> ElementAtAsync(int index, CancellationToken ct = default)
        => query.ElementAtAsync(index, ct);

    public Task<T?> ElementAtOrDefaultAsync(int index, CancellationToken ct = default)
        => query.ElementAtOrDefaultAsync(index, ct);

    public Task<R> MaxAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default)
        => query.MaxAsync(selector, ct);

    public Task<R> MinAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default)
        => query.MinAsync(selector, ct);

    public Task<T> FirstAsync(CancellationToken ct = default)
        => query.FirstAsync(ct);

    public Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.FirstAsync(predicate, ct);

    public Task<T?> FirstOrDefaultAsync(CancellationToken ct = default)
        => query.FirstOrDefaultAsync(ct);

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.FirstOrDefaultAsync(predicate, ct);

    public Task<T> SingleAsync(CancellationToken ct = default)
        => query.SingleAsync(ct);

    public Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.SingleAsync(predicate, ct);

    public Task<T?> SingleOrDefaultAsync(CancellationToken ct = default)
        => query.SingleOrDefaultAsync(ct);

    public Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.SingleOrDefaultAsync(predicate, ct);

    public Task<T> LastAsync(CancellationToken ct = default)
        => query.LastAsync(ct);

    public Task<T> LastAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.LastAsync(predicate, ct);

    public Task<T?> LastOrDefaultAsync(CancellationToken ct = default)
        => query.LastOrDefaultAsync(ct);

    public Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.LastOrDefaultAsync(predicate, ct);

    public Task<T> MinAsync(CancellationToken ct = default)
        => query.MinAsync(ct);

    public Task<T> MaxAsync(CancellationToken ct = default)
        => query.MaxAsync(ct);

    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<decimal?> SumAsync(Expression<Func<T, decimal?>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<int> SumAsync(Expression<Func<T, int>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<int?> SumAsync(Expression<Func<T, int?>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<long> SumAsync(Expression<Func<T, long>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<long?> SumAsync(Expression<Func<T, long?>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<float> SumAsync(Expression<Func<T, float>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<float?> SumAsync(Expression<Func<T, float?>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<double> SumAsync(Expression<Func<T, double>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<double?> SumAsync(Expression<Func<T, double?>> selector, CancellationToken ct = default)
        => query.SumAsync(selector, ct);

    public Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<decimal?> AverageAsync(Expression<Func<T, decimal?>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<float> AverageAsync(Expression<Func<T, float>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<float?> AverageAsync(Expression<Func<T, float?>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<double> AverageAsync(Expression<Func<T, double>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<double?> AverageAsync(Expression<Func<T, double?>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<T[]> ToArrayAsync(CancellationToken ct = default)
        => query.ToArrayAsync(ct);

    public Task<List<T>> ToListAsync(CancellationToken ct = default)
        => query.ToListAsync(ct); 
    #endregion

    #region Sync
    public bool Any() 
        => query.Any();

    public bool Any(Expression<Func<T, bool>> predicate) 
        => query.Any(predicate);

    public bool All(Expression<Func<T, bool>> predicate) 
        => query.All(predicate);

    public int Count() 
        => query.Count();

    public int Count(Expression<Func<T, bool>> predicate) 
        => query.Count(predicate);

    public long LongCount()
    {
        throw new NotImplementedException();
    }

    public long LongCount(Expression<Func<T, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public T ElementAt(int index)
    {
        throw new NotImplementedException();
    }

    public T? ElementAtOrDefault(int index)
    {
        throw new NotImplementedException();
    }

    public T First()
    {
        throw new NotImplementedException();
    }

    public T First(Expression<Func<T, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public T? FirstOrDefault()
    {
        throw new NotImplementedException();
    }

    public T? FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public T Last()
    {
        throw new NotImplementedException();
    }

    public T Last(Expression<Func<T, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public T? LastOrDefault()
    {
        throw new NotImplementedException();
    }

    public T? LastOrDefault(Expression<Func<T, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public T Single()
    {
        throw new NotImplementedException();
    }

    public T Single(Expression<Func<T, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public T? SingleOrDefault()
    {
        throw new NotImplementedException();
    }

    public T? SingleOrDefault(Expression<Func<T, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public T Min()
    {
        throw new NotImplementedException();
    }

    public R Min<R>(Expression<Func<T, R>> selector)
    {
        throw new NotImplementedException();
    }

    public T Max()
    {
        throw new NotImplementedException();
    }

    public R Max<R>(Expression<Func<T, R>> selector)
    {
        throw new NotImplementedException();
    }

    public decimal Sum(Expression<Func<T, decimal>> selector)
    {
        throw new NotImplementedException();
    }

    public decimal? Sum(Expression<Func<T, decimal?>> selector)
    {
        throw new NotImplementedException();
    }

    public int Sum(Expression<Func<T, int>> selector)
    {
        throw new NotImplementedException();
    }

    public int? Sum(Expression<Func<T, int?>> selector)
    {
        throw new NotImplementedException();
    }

    public long Sum(Expression<Func<T, long>> selector)
    {
        throw new NotImplementedException();
    }

    public long? Sum(Expression<Func<T, long?>> selector)
    {
        throw new NotImplementedException();
    }

    public float Sum(Expression<Func<T, float>> selector)
    {
        throw new NotImplementedException();
    }

    public float? Sum(Expression<Func<T, float?>> selector)
    {
        throw new NotImplementedException();
    }

    public double Sum(Expression<Func<T, double>> selector)
    {
        throw new NotImplementedException();
    }

    public double? Sum(Expression<Func<T, double?>> selector)
    {
        throw new NotImplementedException();
    }

    public decimal Average(Expression<Func<T, decimal>> selector)
    {
        throw new NotImplementedException();
    }

    public decimal? Average(Expression<Func<T, decimal?>> selector)
    {
        throw new NotImplementedException();
    }

    public float Average(Expression<Func<T, float>> selector)
    {
        throw new NotImplementedException();
    }

    public float? Average(Expression<Func<T, float?>> selector)
    {
        throw new NotImplementedException();
    }

    public double Average(Expression<Func<T, double>> selector)
    {
        throw new NotImplementedException();
    }

    public double? Average(Expression<Func<T, double?>> selector)
    {
        throw new NotImplementedException();
    }

    public T[] ToArray()
    {
        throw new NotImplementedException();
    }

    public List<T> ToList()
    {
        throw new NotImplementedException();
    } 
    #endregion

    public string ToQueryString(IQuery<T> query)
        => query.ToQueryString();
}
