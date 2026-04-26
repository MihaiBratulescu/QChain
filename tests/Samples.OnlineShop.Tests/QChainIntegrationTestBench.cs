using QChain;
using Samples.OnlineShop.Database;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests;

public class QChainIntegrationTestBench(SqliteFixture fixture) : IClassFixture<SqliteFixture>
{
    protected readonly SqliteFixture _fixture = fixture;

    protected Task<T[]> Query<T>(Func<IUnitOfWork, IQuery<T>> query) =>
        _fixture.db.Query(query).ToArrayAsync(default);
}

public class Map(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task SingleColumn()
    {
        var accounts = await Query(q => q.Accounts.Map(a => a.AccountId));
        Assert.NotEmpty(accounts);
    }

    [Fact]
    public async Task SingleEntity()
    {
        var accounts = await Query(q => q.Accounts.Map(a => new { a.AccountId, a.Name }));
        Assert.NotEmpty(accounts);
    }

    [Fact]
    public async Task Join()
    {
        var results = await Query(q =>
            q.Accounts
             .Join(q.Orders, a => a.AccountId, o => o.AccountId)
             .Map(j => new { j.Item1.AccountId, orderAccountId = j.Item2.AccountId, j.Item2.OrderId }));

        Assert.NotEmpty(results);
        Assert.All(results, q => Assert.Equal(q.AccountId, q.orderAccountId));
    }

    [Fact]
    public async Task GroupJoin()
    {
        var results = await Query(q =>
            q.Accounts
             .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId)
             .Map(j => new 
             { 
                 j.Item1.AccountId, 
                 orderIds = j.Item2.Select(o => ValueTuple.Create(o.OrderId, o.AccountId)) 
             }));

        Assert.NotEmpty(results);
        foreach (var result in results)
        {
            Assert.All(result.orderIds, o => Assert.Equal(result.AccountId, o.Item2));
        }
    }
}

public class GroupJoin(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task OnTable()
    {
        IGrouping<string?, Account>[] result = await Query(q =>
            q.Accounts.GroupBy1(a => a.Name));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, a => Assert.Equal(q.Key, a.Name)));
    }

    [Fact]
    public async Task WithElementSelector()
    {
        var result = await Query(q =>
            q.Accounts
             .GroupBy2(a => a.Name, a => new { a.Name, a.IsActive }));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, a => Assert.Equal(q.Key, a.Name)));
    }

    [Fact]
    public async Task WithProjection()
    {
        (string? name, int count)[] result = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => ValueTuple.Create(g.Key, g.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.count > 0));
    }

    [Fact]
    public async Task WithJoin_Lambda()
    {
        var test = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => new { g.Key, total = g.Count() })
             .Join(q.Accounts, g => g.Key, a => a.Name));
    }

    [Fact]
    public async Task WithJoin_Tuple()
    {
        var test = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => ValueTuple.Create(g.Key, g.Count()))
             .Join(q.Accounts, g => g.Item1, a => a.Name));
    }
}

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

public class Filtering(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task IsApplied()
    {
        var accounts = await Query(q => q.Accounts.Active());

        Assert.NotEmpty(accounts);
        Assert.All(accounts, a => Assert.True(a.IsActive));
    }

    [Fact]
    public async Task NotMatched_ReturnsEmpty()
    {
        var accounts = await Query(q => 
            q.Accounts.Before(DateTime.UtcNow.AddYears(-10)));

        Assert.Empty(accounts);
    }

    [Fact]
    public async Task AfterMap_UsesProjectedShape()
    {
        var rows = await Query(
            q => q.Accounts.Map(a => new
                {
                    Id = a.AccountId,
                    Active = a.IsActive
                })
                .Where(x => x.Active));

        Assert.NotEmpty(rows);
        Assert.All(rows, a => Assert.True(a.Active));
    }
}
