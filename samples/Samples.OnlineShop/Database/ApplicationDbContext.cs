using QChain;

using Microsoft.EntityFrameworkCore;

using Samples.OnlineShop.DatabaseModels;
using Samples.OnlineShop.Repositories;
using QChain.EntityFrameworkCore;

namespace Samples.OnlineShop.Database;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public IAccountsRepository Accounts => new AccountsRepository(Set<Account>());
    public IOrdersRepository Orders => new OrdersRepository(Set<Order>());
    public ITransactionsRepository Transactions => new TransactionsRepository(Set<Transaction>());
    public IQuery<Currency> Currencies => new Query<Currency>(Set<Currency>());

    public T Query<T>(Func<IUnitOfWork, T> query) => query(this);
    public Task<T> Query<T>(Func<IUnitOfWork, Task<T>> query) => query(this);
    public IQueryExecutor<T> Query<T>(Func<IUnitOfWork, IQuery<T>> query) => new QueryExecutor<T>(query(this));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>();
        modelBuilder.Entity<AccountProfile>();
        modelBuilder.Entity<Order>();
        modelBuilder.Entity<Transaction>();
        modelBuilder.Entity<Currency>();
    }
}
