using PCompose;
using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

public partial class QueryExecutor<T> : IQueryExecutor<T>
{
    public Task<bool> AllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.AllAsync(predicate, ct);

    public Task<bool> AllAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => AllAsync(predicate.Compile(), ct);

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => query.AnyAsync(ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.AnyAsync(predicate, ct);

    public Task<bool> AnyAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => AnyAsync(predicate.Compile(), ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => query.CountAsync(ct);

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.CountAsync(predicate, ct);

    public Task<int> CountAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => CountAsync(predicate.Compile(), ct);

    public Task<long> LongCountAsync(CancellationToken ct = default)
        => query.LongCountAsync(ct);

    public Task<long> LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.LongCountAsync(predicate, ct);

    public Task<long> LongCountAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => LongCountAsync(predicate.Compile(), ct);

    public Task<T> ElementAtAsync(int index, CancellationToken ct = default)
        => query.ElementAtAsync(index, ct);

    public Task<T?> ElementAtOrDefaultAsync(int index, CancellationToken ct = default)
        => query.ElementAtOrDefaultAsync(index, ct);

    public Task<R?> MaxAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default)
        => query.MaxAsync(selector, ct);

    public Task<R?> MinAsync<R>(Expression<Func<T, R>> selector, CancellationToken ct = default)
        => query.MinAsync(selector, ct);

    public Task<bool> ContainsAsync(T item, CancellationToken ct = default)
        => query.ContainsAsync(item, ct);

    public Task<T> FirstAsync(CancellationToken ct = default)
        => query.FirstAsync(ct);

    public Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.FirstAsync(predicate, ct);

    public Task<T> FirstAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => FirstAsync(predicate.Compile(), ct);

    public Task<T?> FirstOrDefaultAsync(CancellationToken ct = default)
        => query.FirstOrDefaultAsync(ct);

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.FirstOrDefaultAsync(predicate, ct);

    public Task<T?> FirstOrDefaultAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => FirstOrDefaultAsync(predicate.Compile(), ct);

    public Task<T> SingleAsync(CancellationToken ct = default)
        => query.SingleAsync(ct);

    public Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.SingleAsync(predicate, ct);

    public Task<T> SingleAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => SingleAsync(predicate.Compile(), ct);

    public Task<T?> SingleOrDefaultAsync(CancellationToken ct = default)
        => query.SingleOrDefaultAsync(ct);

    public Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.SingleOrDefaultAsync(predicate, ct);

    public Task<T?> SingleOrDefaultAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => SingleOrDefaultAsync(predicate.Compile(), ct);

    public Task<T> LastAsync(CancellationToken ct = default)
        => query.LastAsync(ct);

    public Task<T> LastAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.LastAsync(predicate, ct);

    public Task<T> LastAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => LastAsync(predicate.Compile(), ct);

    public Task<T?> LastOrDefaultAsync(CancellationToken ct = default)
        => query.LastOrDefaultAsync(ct);

    public Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.LastOrDefaultAsync(predicate, ct);

    public Task<T?> LastOrDefaultAsync(Func<T, Predicate> predicate, CancellationToken ct = default)
        => LastOrDefaultAsync(predicate.Compile(), ct);

    public Task<T?> MinAsync(CancellationToken ct = default)
        => query.MinAsync(ct);

    public Task<T?> MaxAsync(CancellationToken ct = default)
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

    public Task<double> AverageAsync(Expression<Func<T, int>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<double?> AverageAsync(Expression<Func<T, int?>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<double> AverageAsync(Expression<Func<T, long>> selector, CancellationToken ct = default)
        => query.AverageAsync(selector, ct);

    public Task<double?> AverageAsync(Expression<Func<T, long?>> selector, CancellationToken ct = default)
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
}
