using Microsoft.EntityFrameworkCore;
using QChain;
using QChain.EntityFrameworkCore;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Repositories;

public interface IAccountsRepository : IQuery<Account>
{
    IAccountsRepository Active();
}

public class AccountsRepository(IQueryable<Account> query) : EntityQuery<Account>(query), IAccountsRepository
{
    private AccountsRepository(IQuery<Account> query) : this(query.AsQueryable())
    {
    }

    public Task<Account?> Find(int key) => AsQueryable().FirstOrDefaultAsync(a => a.AccountId == key);

    public IAccountsRepository Active() =>
        new AccountsRepository(Where(a => a.IsActive));

    public IQuery<(Account account, IEnumerable<Order> orders)> WithOrders(IQuery<Order> orders) =>
        GroupJoin(orders, a => a.AccountId, o => o.AccountId);
}

public interface IOrdersRepository : IQuery<Order>
{
    IOrdersRepository InLastMonth();
}

public class OrdersRepository(IQueryable<Order> query) : EntityQuery<Order>(query), IOrdersRepository
{
    private OrdersRepository(IQuery<Order> query) : this(query.AsQueryable())
    {
    }

    public IOrdersRepository InLastMonth() =>
        new OrdersRepository(Where(o => o.CreatedDate < DateTime.UtcNow.AddMonths(-1)));
}

public interface ITransactionsRepository : IQuery<Transaction>
{
    ITransactionsRepository Settled();
}

public class TransactionsRepository(IQueryable<Transaction> query) : EntityQuery<Transaction>(query), ITransactionsRepository
{
    private TransactionsRepository(IQuery<Transaction> query) : this(query.AsQueryable())
    {
    }

    public ITransactionsRepository Settled() =>
        new TransactionsRepository(Where(t => t.Status == TransactionStatus.Settled));
}
