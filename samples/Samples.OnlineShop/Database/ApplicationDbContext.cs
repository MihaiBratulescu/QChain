using QChain;

using Microsoft.EntityFrameworkCore;

using Samples.OnlineShop.DatabaseModels;
using Samples.OnlineShop.Repositories;

namespace Samples.OnlineShop.Database;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public IAccountsRepository Accounts => new AccountsRepository(Set<Account>());
    public IOrdersRepository Orders => new OrdersRepository(Set<Order>());
    public ITransactionsRepository Transactions => new TransactionsRepository(Set<Transaction>());
    public IQuery<Currency> Currencies => new Query<Currency>(Set<Currency>());

    public IQuery<T> Query<T>(Func<IUnitOfWork, IQuery<T>> query) => query(this);

    #region DbSets
    private DbSet<Account> _accounts { get; set; }
    private DbSet<AccountProfile> _accountProfiles { get; set; }
    private DbSet<Order> _orders { get; set; }
    private DbSet<Transaction> _transactions { get; set; }
    private DbSet<Currency> _currencies { get; set; }
    #endregion
}
