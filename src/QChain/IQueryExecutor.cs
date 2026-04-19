using System.Linq.Expressions;

namespace Common.Query;

public interface IQueryExecutor<T>
{
    Task<T?> FirstOrDefault(CancellationToken ct);
    Task<T?> FirstOrDefault(Expression<Func<T, bool>> selector, CancellationToken ct);

    Task<bool> AnyAsync(CancellationToken ct);
    Task<bool> AnyAsync(Expression<Func<T, bool>> selector, CancellationToken ct);

    Task<int> CountAsync(CancellationToken ct);
    Task<int> CountAsync(Expression<Func<T, bool>> selector, CancellationToken ct);

    Task<T[]> ToArrayAsync(CancellationToken ct);
}