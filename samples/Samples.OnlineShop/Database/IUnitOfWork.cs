namespace Samples.OnlineShop.Database;

public interface IUnitOfWork
{
    public T Query<T>(Func<IUnitOfWork, T> query);
    public Task<T> Query<T>(Func<IUnitOfWork, Task<T>> query);
    
    Task<int> SaveChangesAsync(CancellationToken ct);
}
