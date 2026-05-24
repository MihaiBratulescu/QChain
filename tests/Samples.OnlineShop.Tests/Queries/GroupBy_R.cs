using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class GroupBy_R(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Key_Grouping_Direct()
    {
        var scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                g => new { CurrencyId = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) }));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.True(x.Count > 0));

        var tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                g => ValueTuple.Create(g.Key, g.Count())));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public async Task Key_Grouping_Join()
    {
        (CurrencyType currencyId, int count, Currency currency)[] scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                g => new { CurrencyId = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) })
            .Join(
                q.Currencies,
                g => g.CurrencyId,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.CurrencyId, g.Count, c)));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.Equal(x.currencyId, x.currency.CurrencyId));

        ((CurrencyType currencyId, int accountId) key, int count, Currency currency)[] tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                g => ValueTuple.Create(g.Key, g.Count()))
            .Join(
                q.Currencies,
                g => g.Item1.Item1,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.Item1, g.Item2, c)));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.Equal(x.key.currencyId, x.currency.CurrencyId));
    }

    [Fact]
    public async Task Key_Selector_Direct()
    {
        var scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                (key, items) => new { CurrencyId = key, Count = items.Count(), Total = items.Sum(o => o.Total) }));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.True(x.Count > 0));

        var tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                (key, items) => ValueTuple.Create(key, items.Count())));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public async Task Key_Selector_Join()
    {
        (CurrencyType currencyId, int count, Currency currency)[] scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                (key, items) => new { CurrencyId = key, Count = items.Count(), Total = items.Sum(o => o.Total) })
            .Join(
                q.Currencies,
                g => g.CurrencyId,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.CurrencyId, g.Count, c)));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.Equal(x.currencyId, x.currency.CurrencyId));

        ((CurrencyType currencyId, int accountId) key, int count, Currency currency)[] tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                (key, items) => ValueTuple.Create(key, items.Count()))
            .Join(
                q.Currencies,
                g => g.Item1.Item1,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.Item1, g.Item2, c)));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.Equal(x.key.currencyId, x.currency.CurrencyId));
    }

    [Fact]
    public async Task Key_Element_Selector_Direct()
    {
        var scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                o => new { o.Total, o.AccountId },
                (key, items) => new { CurrencyId = key, Count = items.Count(), Total = items.Sum(o => o.Total) }));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.True(x.Count > 0));

        var tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                o => new { o.Total, o.OrderId },
                (key, items) => ValueTuple.Create(key, items.Count())));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public async Task Key_Element_Selector_Join()
    {
        (CurrencyType currencyId, int count, Currency currency)[] scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                o => new { o.Total, o.AccountId },
                (key, items) => new { CurrencyId = key, Count = items.Count(), Total = items.Sum(o => o.Total) })
            .Join(
                q.Currencies,
                g => g.CurrencyId,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.CurrencyId, g.Count, c)));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.Equal(x.currencyId, x.currency.CurrencyId));

        ((CurrencyType currencyId, int accountId) key, int count, Currency currency)[] tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                o => new { o.Total, o.OrderId },
                (key, items) => ValueTuple.Create(key, items.Count()))
            .Join(
                q.Currencies,
                g => g.Item1.Item1,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.Item1, g.Item2, c)));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.Equal(x.key.currencyId, x.currency.CurrencyId));
    }
}
