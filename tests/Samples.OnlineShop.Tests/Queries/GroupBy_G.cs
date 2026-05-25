using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class GroupBy_G(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task OnTable()
    {
        var result = await Query(q =>
            q.Accounts.GroupBy(a => a.Email));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, a => Assert.Equal(q.Key, a.Email)));
    }

    [Fact]
    public async Task OnTuple()
    {
        var result = await Query(q =>
            q.Accounts
             .Select(a => ValueTuple.Create(a.Email, a.IsActive))
             .GroupBy(a => a.Item1));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, a => Assert.Equal(q.Key, a.Item1)));
    }

    [Fact]
    public async Task TupleKey()
    {
        var result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Email, a.IsActive)));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, a => Assert.Equal(q.Key.Item1, a.Email)));
    }

    [Fact]
    public async Task ElementSelector_OnTuple()
    {
        var result = await Query(q =>
            q.Orders.GroupBy(
                o => o.CurrencyId,
                o => ValueTuple.Create(o.OrderId, o.Total)));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, o => Assert.True(o.Item1 > 0)));
    }

    [Fact]
    public async Task TupleKey_ElementSelector_OnTuple()
    {
        var result = await Query(q =>
            q.Orders.GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                o => ValueTuple.Create(o.OrderId, o.Total)));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, o => Assert.True(o.Item1 > 0)));
    }

    [Fact]
    public async Task ElementSelector_Select_ToArray()
    {
        (CurrencyType key, (int orderId, decimal total)[] items)[] result = await Query(q =>
            q.Orders
                .GroupBy(
                    o => o.CurrencyId,
                    o => ValueTuple.Create(o.OrderId, o.Total))
                .Select(g => ValueTuple.Create(g.Key, g.ToArray())));

        Assert.NotEmpty(result);
        Assert.All(result, q =>
        {
            Assert.NotEmpty(q.items);
            Assert.All(q.items, o => Assert.True(o.orderId > 0));
        });
    }

    [Fact]
    public async Task TupleKey_Projected()
    {
        ((string?, bool), int total)[] result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Email, a.IsActive),
                               g => ValueTuple.Create(g.Key, g.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.total > 0));
    }

    [Fact]
    public async Task WithProjection()
    {
        (string? name, int count)[] result = await Query(q =>
            q.Accounts.GroupBy(a => a.Email, a => ValueTuple.Create(a.Key, a.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.count > 0) );
    }

    [Fact]
    public async Task WithJoin()
    {
        var result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Email, a.IsActive),
                               g => new { g.Key, total = g.Count(), first = g.Min(a => a.AccountId) })
                      .GroupJoin(q.Orders, g => g.first, o => o.AccountId));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Join_GroupOnTupleKey()
    {
        var result = await Query(q => q.Orders
            .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
            .GroupBy(x => ValueTuple.Create(x.Item1.CurrencyId, x.Item2.AccountId)));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Join_GroupOnTupleKey_ValueTuple()
    {
        var result = await Query(q => q.Orders
            .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
            .GroupBy(x => new ValueTuple<CurrencyType, int>(x.Item1.CurrencyId, x.Item2.AccountId)));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GroupBy_ThenJoin_OnKey()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.CurrencyId)
            .Select(g => ValueTuple.Create(g.Key, g.Count()))
            .Join(
                q.Currencies,
                x => x.Item1,
                c => c.CurrencyId));

        Assert.NotEmpty(result);
        Assert.All(result, x => Assert.Equal(x.Item1.Item1, x.Item2.CurrencyId));
        Assert.All(result, x => Assert.True(x.Item1.Item2 > 0));
    }

    [Fact]
    public async Task TupleKey_GroupBy_ThenJoin_OnKeyMember()
    {
        (((CurrencyType, int), int), Currency)[] result = await Query(q => q.Orders
            .GroupBy(o => ValueTuple.Create(o.CurrencyId, o.AccountId))
            .Select(g => ValueTuple.Create(g.Key, g.Count()))
            .Join(
                q.Currencies,
                x => x.Item1.Item1,
                c => c.CurrencyId));

        Assert.NotEmpty(result);
        Assert.All(result, x => Assert.Equal(x.Item1.Item1.Item1, x.Item2.CurrencyId));
        Assert.All(result, x => Assert.True(x.Item1.Item2 > 0));
    }

    [Fact]
    public async Task Aggregate_ThenJoin()
    {
        (CurrencyType currencyId, int activeCount, Currency currency)[] result =
            await Query(q => q.Orders
                .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
                .GroupBy(
                    x => new { x.Item1.CurrencyId, x.Item2.AccountId },
                    g => new
                    {
                        g.Key,
                        ActiveCount = g.Count(x => x.Item2.IsActive)
                    })
                .Join(q.Currencies,
                    g => g.Key.CurrencyId,
                    c => c.CurrencyId,
                    (g, c) => ValueTuple.Create(g.Key.CurrencyId, g.ActiveCount, c)));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task TupleMap_AfterGroupJoin()
    {
        (CurrencyType currencyId, int activeCount, Currency currency)[] result =
            await Query(q => q.Orders
                .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
                .GroupBy(
                    x => new { x.Item1.CurrencyId },
                    g => new
                    {
                        g.Key.CurrencyId,
                        ActiveCount = g.Count(x => x.Item2.IsActive)
                    })
                .Join(q.Currencies,
                    g => g.CurrencyId,
                    c => c.CurrencyId,
                    (g, c) => new { g.CurrencyId, g.ActiveCount, Currency = c })
                .Select(x => ValueTuple.Create(x.CurrencyId, x.ActiveCount, x.Currency)));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Projection_WithSum()
    {
        var result = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                g => ValueTuple.Create(
                    g.Key,
                    g.Sum(o => o.Total),
                    g.Min(o => o.Total),
                    g.Max(o => o.Total))));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task TupleKey_Project_ThenJoin_OnKeyMember()
    {
        var result = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                g => ValueTuple.Create(g.Key, g.Count()))
            .Join(
                q.Currencies,
                x => x.Item1.Item1,
                c => c.CurrencyId));

        Assert.NotEmpty(result);
        Assert.All(result, x => Assert.Equal(x.Item1.Item1.Item1, x.Item2.CurrencyId));
    }

    [Fact]
    public async Task Projection_WithFilteredCount()
    {
        var result = await Query(q => q.Accounts
            .GroupBy(
                a => a.IsActive,
                g => ValueTuple.Create(
                    g.Key,
                    g.Count(a => a.Email != null))));

        Assert.NotEmpty(result);
        Assert.All(result, x => Assert.True(x.Item2 >= 0));
    }
}
