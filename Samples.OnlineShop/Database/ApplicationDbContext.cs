using QChain;
using QChain.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

using Samples.OnlineShop.DatabaseModels;
using Samples.OnlineShop.Repositories;

namespace Samples.OnlineShop.Database;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    public IAccountsRepository Accounts => new AccountsRepository(Set<Account>());
    public IOrdersRepository Orders => new OrdersRepository(Set<Order>());
    public ITransactionsRepository Transactions => new TransactionsRepository(Set<Transaction>());
    public IQuery<Currency> Currencies => new EntityQuery<Currency>(Set<Currency>());

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public IQueryExecutor<T> Query<T>(Func<IUnitOfWork, IQuery<T>> query) =>
        new QueryExecutor<T>(query(this));

    #region DbSets
    private DbSet<Account> _accounts { get; set; }
    private DbSet<Order> _orders { get; set; }
    private DbSet<Transaction> _transactions { get; set; }
    private DbSet<Currency> _currencies { get; set; }
    #endregion
}
