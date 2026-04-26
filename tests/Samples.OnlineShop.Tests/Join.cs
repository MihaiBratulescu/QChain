using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests;

public class Join(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task TwoTables()
    {
        (Account account, Order order)[] result = await Query(q => 
            q.Accounts
             .Join(q.Orders, a => a.AccountId, o => o.AccountId));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.Equal(q.account.AccountId, q.order.AccountId));
    }

    [Fact]
    public async Task ThreeTables()
    {
        (Account account, Order order, Transaction transaction)[] result = await Query(q =>
            q.Accounts
             .Join(q.Orders, a => a.AccountId, o => o.AccountId)
             .Join(q.Transactions, j => j.Item2.OrderId, t => t.OrderId, 
                (j, t) => ValueTuple.Create(j.Item1, j.Item2, t)));

        Assert.NotEmpty(result);
        Assert.All(result, q =>
        {
            Assert.Equal(q.account.AccountId, q.order.AccountId);
            Assert.Equal(q.order.OrderId, q.transaction.OrderId);
        });
    }

    [Fact]
    public async Task GroupJoin()
    {
        (Account account, IEnumerable<Order> orders)[] result = await Query(q =>
            q.Accounts
             .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId));

        Assert.NotEmpty(result);
        foreach (var (account, orders) in result)
        {
            Assert.All(orders, o => Assert.Equal(account.AccountId, o.AccountId));
        }
    }
}
