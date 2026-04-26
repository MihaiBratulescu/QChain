using Microsoft.EntityFrameworkCore;
using QChain;
using QChain.EntityFrameworkCore;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Repositories;

public interface IAccountsRepository : IQuery<Account>
{
    IAccountsRepository Active();
    IAccountsRepository Before(DateTime dateTime);
    IAccountsRepository FromEurope();

    IQuery<(Account account, IEnumerable<Order> orders)> WithOrders(IQuery<Order> orders);
}

public class AccountsRepository(IQueryable<Account> query) : EntityQuery<Account>(query), IAccountsRepository
{
    private AccountsRepository(IQuery<Account> query) : this(query.AsQueryable())
    {
    }

    public Task<Account?> Find(int key) => AsQueryable().FirstOrDefaultAsync(a => a.AccountId == key);

    public IAccountsRepository Active() =>
        new AccountsRepository(Where(a => a.IsActive));

    public IAccountsRepository Before(DateTime dateTime) =>
        new AccountsRepository(Where(a => a.CreatedDate < dateTime));

    public IAccountsRepository FromEurope() =>
        new AccountsRepository(Where(a => true));

    public IQuery<(Account account, IEnumerable<Order> orders)> WithOrders(IQuery<Order> orders) =>
        GroupJoin(orders, a => a.AccountId, o => o.AccountId);
}

public interface IOrdersRepository : IQuery<Order>
{
    IOrdersRepository InLastMonth();
    IOrdersRepository NewestFirst();
    IOrdersRepository WithCurrencies(params CurrencyType[] currencies);
    IQuery<(Order order, IEnumerable<Transaction> transactions)> WithTransactions(IQuery<Transaction> transactions);
}

public class OrdersRepository(IQueryable<Order> query) : EntityQuery<Order>(query), IOrdersRepository
{
    private OrdersRepository(IQuery<Order> query) : this(query.AsQueryable())
    {
    }

    public IOrdersRepository InLastMonth() =>
        new OrdersRepository(Where(o => o.CreatedDate >= DateTime.UtcNow.AddMonths(-1)));

    public IOrdersRepository NewestFirst() =>
        new OrdersRepository(OrderByDescending(o => o.CreatedDate));

    public IOrdersRepository WithCurrencies(params CurrencyType[] currencies) =>
        new OrdersRepository(Where(o => currencies.Contains(o.CurrencyId)));

    public IQuery<(Order order, IEnumerable<Transaction> transactions)> WithTransactions(IQuery<Transaction> transactions) =>
        GroupJoin(transactions, o => o.OrderId, t => t.TransactionId);
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
