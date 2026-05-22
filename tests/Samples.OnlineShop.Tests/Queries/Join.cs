using QChain;
using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class GroupJoin(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Preserves_Outer_Items()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId)
                .Select(x => ValueTuple.Create(
                    x.Item1.AccountId,
                    x.Item2.Count())));

        var accountCount = await _fixture.db.Accounts.CountAsync();

        Assert.Equal(accountCount, rows.Length);
        Assert.All(rows, x => Assert.True(x.Item1 > 0));
        Assert.All(rows, x => Assert.True(x.Item2 >= 0));
    }

    [Fact]
    public async Task Custom_ResultSelector_Preserves_Group()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(
                    q.Orders,
                    a => a.AccountId,
                    o => o.AccountId,
                    (a, orders) => ValueTuple.Create(
                        a.AccountId,
                        orders.Count()))
                .OrderBy(x => x.Item1));

        var accountCount = await _fixture.db.Accounts.CountAsync();

        Assert.Equal(accountCount, rows.Length);
        Assert.All(rows, x => Assert.True(x.Item1 > 0));
        Assert.All(rows, x => Assert.True(x.Item2 >= 0));
    }

    [Fact]
    public async Task GroupJoin_And_SelectMany_Are_Equivalent()
    {
        var flattened = await Query(q => q.Accounts
            .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .SelectMany(x => x.Item2)
            .Select(o => o.OrderId));

        var joined = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => x.Item2.OrderId));

        Assert.Equal(
            joined.OrderBy(x => x),
            flattened.OrderBy(x => x));
    }
}

public class Join(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
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
