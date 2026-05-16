using QChain;
using QChain.EntityFrameworkCore;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Repositories;

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
