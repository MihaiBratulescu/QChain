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
            .Where(a => a.AccountId < 0)
            .RightJoin(q.Orders, a => a.AccountId, o => o.AccountId));

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
            .Where(a => a.AccountId < 0)
            .RightJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => new
            {
                AccountId = x.Item1 == null ? (int?)null : x.Item1.AccountId,
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
}

public class LeftJoin(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Basic_ReturnsAccountsWithOrders()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId));

        Assert.NotEmpty(items);
        Assert.Contains(items, x => x.Item1 is not null);
    }

    [Fact]
    public async Task Basic_ReturnsAccountsWithoutOrders()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId));

        Assert.Contains(items, x => x.Item2 is null);
    }

    [Fact]
    public async Task CanFilterLeftSide()
    {
        var items = await Query(q => q.Accounts
            .Where(a => a.IsActive)
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.True(x.Item1.IsActive));
    }

    [Fact]
    public async Task CanFilterRightSideBeforeJoin()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(
                q.Orders.Where(o => o.CurrencyId == CurrencyType.EUR),
                a => a.AccountId, o => o.AccountId));

        Assert.NotEmpty(items);

        Assert.All(items.Where(x => x.Item2 is not null), 
            x => Assert.Equal(CurrencyType.EUR, x.Item2!.CurrencyId));
    }

    [Fact]
    public async Task CanProjectResult()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => new
            {
                x.Item1.AccountId,
                OrderId = x.Item2 == null ? (int?)null : x.Item2.OrderId
            }));

        Assert.NotEmpty(items);
        Assert.Contains(items, x => x.OrderId is null);
    }

    [Fact]
    public async Task CanUsePredicatesOnLeftAndRight()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .Where(x => x.Item1.IsActive));

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task LeftJoin_Twice()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .LeftJoin(q.Currencies, x => x.Item2!.CurrencyId, c => c.CurrencyId));

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task LeftJoin_Twice_WithProjection()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .LeftJoin(q.Currencies, x => x.Item2!.CurrencyId, c => c.CurrencyId)
            .Select(x => new
            {
                AccountId = x.Item1.Item1.AccountId,
                OrderId = x.Item1.Item2 == null
                    ? (int?)null
                    : x.Item1.Item2.OrderId,
                Currency = x.Item2 == null
                    ? null
                    : x.Item2.Symbol
            }));

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task LeftJoin_LeftJoin_RightJoin()
    {
        var items = await Query(q => q.Accounts
            .LeftJoin(q.Orders, a => a.AccountId, o => o.AccountId)
            .LeftJoin(q.Currencies, x => x.Item2!.CurrencyId, c => c.CurrencyId)
            .RightJoin(q.Transactions, x => x.Item1.Item2!.OrderId, t => t.OrderId));

        Assert.NotEmpty(items);
    }
}
