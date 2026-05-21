using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class RightJoin(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Basic_ReturnsOrdersWithAccounts()
    {
        var items = await Query(q => q.Accounts
            .RightJoin(q.Orders, a => a.AccountId, o => o.AccountId));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.NotNull(x.Item2));
    }

    [Fact]
    public async Task Basic_ReturnsOrdersWithoutAccounts()
    {
        var items = await Query(q => q.Accounts
            .RightJoin(q.Orders, a => 0, o => o.AccountId));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.Null(x.Item1));
        Assert.All(items, x => Assert.NotNull(x.Item2));
    }

    [Fact]
    public async Task CanFilterRightSideBeforeJoin()
    {
        var items = await Query(q => q.Accounts
            .RightJoin(
                q.Orders.Where(o => o.CurrencyId == CurrencyType.EUR),
                a => a.AccountId, o => o.AccountId));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.Equal(CurrencyType.EUR, x.Item2.CurrencyId));
    }

    [Fact]
    public async Task CanProjectResult()
    {
        var items = await Query(q => q.Accounts
            .RightJoin(q.Orders, a => 0, o => o.AccountId)
            .Select(x => new
            {
                AccountId = x.Item1 == null 
                    ? (int?)null 
                    : x.Item1.AccountId,
                x.Item2.OrderId
            }));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.Null(x.AccountId));
    }

    [Fact]
    public async Task RightJoin_Twice()
    {
        var items = await Query(q => q.Accounts
            .RightJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .RightJoin(q.Currencies, x => x.Item2.CurrencyId, c => c.CurrencyId));

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task RightJoin_Twice_WithProjection()
    {
        var items = await Query(q => q.Accounts
            .RightJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .RightJoin(q.Currencies, x => x.Item2.CurrencyId, c => c.CurrencyId)
            .Select(x => new
            {
                AccountId = x.Item1.Item1 == null 
                    ? (int?)null 
                    : x.Item1.Item1.AccountId,
                x.Item1.Item2.OrderId,
                x.Item2.CurrencyId
            }));

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task LeftJoin_Then_RightJoin()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .RightJoin(q.Currencies, x => x.Item2!.CurrencyId, c => c.CurrencyId));

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task RightJoin_Then_RightJoin()
    {
        var items = await Query(q => q.Accounts
            .RightJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .RightJoin(q.Transactions, x => x.Item2.OrderId, t => t.OrderId));

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task JoinDoesNotMatch_ReturnsNullRight()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => 0));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.Null(x.Item2));
    }
}
