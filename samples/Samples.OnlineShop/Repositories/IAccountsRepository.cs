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

public class AccountsRepository(IQueryable<Account> query) : Query<Account>(query), IAccountsRepository
{
    private AccountsRepository(IQuery<Account> query) : this(query.AsQueryable())
    {
    }

    public IAccountsRepository Active() =>
        new AccountsRepository(Where(a => a.IsActive));

    public IAccountsRepository Before(DateTime dateTime) =>
        new AccountsRepository(Where(a => a.CreatedDate < dateTime));

    public IAccountsRepository FromEurope() =>
        new AccountsRepository(Where(a => true));

    public IQuery<(Account account, IEnumerable<Order> orders)> WithOrders(IQuery<Order> orders) =>
        GroupJoin(orders, a => a.AccountId, o => o.AccountId);
}
