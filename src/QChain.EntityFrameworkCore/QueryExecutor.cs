

using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

internal class QueryExecutor<T> : IQueryExecutor<T>
{
    public Task<bool> AllAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
       => query.AllAsync(predicate, ct);

    public Task<bool> AnyAsync(IQuery<T> query, CancellationToken ct = default)
        => query.AnyAsync(ct);

    public Task<bool> AnyAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.AnyAsync(predicate, ct);

    public Task<int> CountAsync(IQuery<T> query, CancellationToken ct = default)
        => query.CountAsync(ct);

    public Task<int> CountAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.CountAsync(predicate, ct);

    public Task<long> LongCountAsync(IQuery<T> query, CancellationToken ct = default)
        => query.LongCountAsync(ct);

    public Task<long> LongCountAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.LongCountAsync(predicate, ct);

    public Task<T> ElementAtAsync(IQuery<T> query, int index, CancellationToken ct = default)
        => query.ElementAtAsync(index, ct);

    public Task<T?> ElementAtOrDefaultAsync(IQuery<T> query, int index, CancellationToken ct = default)
        => query.ElementAtOrDefaultAsync(index, ct);

    public Task<R> MaxAsync<R>(IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default)
        => query.MaxAsync(selector, ct);

    public Task<R> MinAsync<R>(IQuery<T> query, Expression<Func<T, R>> selector, CancellationToken ct = default)
        => query.MinAsync(selector, ct);

    public Task<T> FirstAsync(IQuery<T> query, CancellationToken ct = default)
        => query.FirstAsync(ct);

    public Task<T> FirstAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.FirstAsync(predicate, ct);

    public Task<T?> FirstOrDefaultAsync(IQuery<T> query, CancellationToken ct = default)
        => query.FirstOrDefaultAsync(ct);

    public Task<T?> FirstOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.FirstOrDefaultAsync(predicate, ct);

    public Task<T> SingleAsync(IQuery<T> query, CancellationToken ct = default)
        => query.SingleAsync(ct);

    public Task<T> SingleAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.SingleAsync(predicate, ct);

    public Task<T?> SingleOrDefaultAsync(IQuery<T> query, CancellationToken ct = default)
        => query.SingleOrDefaultAsync(ct);

    public Task<T?> SingleOrDefaultAsync(IQuery<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => query.SingleOrDefaultAsync(predicate, ct);


    public Task<T[]> ToArrayAsync(IQuery<T> query, CancellationToken ct = default)
        => query.ToArrayAsync(ct);

    public Task<List<T>> ToListAsync(IQuery<T> query, CancellationToken ct = default)
        => query.ToListAsync(ct);

    public string ToQueryString(IQuery<T> query)
        => query.ToQueryString();
}
