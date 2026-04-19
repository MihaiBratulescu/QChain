using Common.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

public class QueryExecutor<T>(IQuery<T> query) : IQueryExecutor<T>
{
    public Task<T?> FirstOrDefault(CancellationToken ct) => Queriable(q => q.FirstOrDefaultAsync(ct));
    public Task<T?> FirstOrDefault(Expression<Func<T, bool>> selector, CancellationToken ct) => Queriable(selector, q => q.FirstOrDefaultAsync(ct));

    public Task<T?> SingleOrDefault(CancellationToken ct) => Queriable(q => q.SingleOrDefaultAsync(ct));
    public Task<T?> SingleOrDefault(Expression<Func<T, bool>> selector, CancellationToken ct) => Queriable(selector, q => q.SingleOrDefaultAsync(ct));

    public Task<int> CountAsync(CancellationToken ct) => Queriable(q => q.CountAsync(ct));
    public Task<int> CountAsync(Expression<Func<T, bool>> selector, CancellationToken ct) => Queriable(selector, q => q.CountAsync(ct));

    public Task<bool> AnyAsync(CancellationToken ct) => Queriable(q => q.AnyAsync(ct));
    public Task<bool> AnyAsync(Expression<Func<T, bool>> selector, CancellationToken ct) => Queriable(selector, q => q.AnyAsync(ct));

    public Task<T[]> ToArrayAsync(CancellationToken ct) => Queriable(q => q.ToArrayAsync(ct));

    private Task<R> Queriable<R>(Expression<Func<T, bool>> selector, Func<IQueryable<T>, Task<R>> executor)
    {
        query = query.Where(selector);
        return Queriable(executor);
    }

    private Task<R> Queriable<R>(Func<IQueryable<T>, Task<R>> executor) => query switch
    {
        //ICachedQuery<R> cached => query.AsQueryable().WithCaching(cache, cached.Key, cached.Expiry, executor),
        //_ when caching.key != null => query.AsQueryable().WithCaching(cache, caching.key, caching.expiry, executor),
        _ => executor(query.AsQueryable())
    };
}
