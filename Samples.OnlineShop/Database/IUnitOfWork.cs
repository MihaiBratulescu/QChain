using QChain;
using Samples.OnlineShop.DatabaseModels;
using Samples.OnlineShop.Repositories;

namespace Samples.OnlineShop.Database;

public interface IUnitOfWork
{
    public IAccountsRepository Accounts { get; }
    public IOrdersRepository Orders { get; }
    public ITransactionsRepository Transactions { get; }
    public IQuery<Currency> Currencies { get; }

    public IQueryExecutor<T> Query<T>(Func<IUnitOfWork, IQuery<T>> query);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
