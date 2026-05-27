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

    [Fact]
    public async Task Key_Grouping_Where_OrderBy()
    {
        var scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                g => new { CurrencyId = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) })
            .Where(g => g.Count > 1)
            .OrderBy(g => g.CurrencyId));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.True(x.Count > 1));

        var tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                g => ValueTuple.Create(g.Key, g.Count()))
            .Where(g => g.Item2 > 0)
            .OrderBy(g => g.Item1.Item1));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public async Task Key_Selector_Where_OrderBy()
    {
        var scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                (key, items) => new { CurrencyId = key, Count = items.Count(), Total = items.Sum(o => o.Total) })
            .Where(g => g.Count > 1)
            .OrderBy(g => g.CurrencyId));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.True(x.Count > 1));

        var tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                (key, items) => ValueTuple.Create(key, items.Count()))
            .Where(g => g.Item2 > 0)
            .OrderBy(g => g.Item1.Item1));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public async Task Key_Element_Selector_Where_OrderBy()
    {
        var scalarRows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                o => new { o.Total, o.AccountId },
                (key, items) => new { CurrencyId = key, Count = items.Count(), Total = items.Sum(o => o.Total) })
            .Where(g => g.Count > 1)
            .OrderBy(g => g.CurrencyId));

        Assert.NotEmpty(scalarRows);
        Assert.All(scalarRows, x => Assert.True(x.Count > 1));

        var tupleRows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                o => new { o.Total, o.OrderId },
                (key, items) => ValueTuple.Create(key, items.Count()))
            .Where(g => g.Item2 > 0)
            .OrderBy(g => g.Item1.Item1));

        Assert.NotEmpty(tupleRows);
        Assert.All(tupleRows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public async Task TupleArity7_Grouping()
    {
        var rows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                g => ValueTuple.Create(
                    g.Key,
                    g.Count(),
                    g.Sum(o => o.Total),
                    g.Min(o => o.Total),
                    g.Max(o => o.Total),
                    g.Count(o => o.Total > 50),
                    g.Max(o => o.AccountId)))
            .Where(g => g.Item2 > 0)
            .OrderBy(g => g.Item1)
            .ThenBy(g => g.Item7));

        Assert.NotEmpty(rows);
        Assert.All(rows, x =>
        {
            Assert.True(x.Item2 > 0);
            Assert.True(x.Item6 >= 0);
            Assert.True(x.Item7 > 0);
        });
    }

    [Fact]
    public async Task TupleKeyArity7_Grouping_Join()
    {
        var rows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(
                    o.CurrencyId,
                    o.AccountId,
                    o.OrderId,
                    o.Total,
                    o.CreatedDate.Year,
                    o.CreatedDate.Month,
                    o.CreatedDate.Day),
                g => ValueTuple.Create(g.Key, g.Count()))
            .Where(g => g.Item2 > 0)
            .OrderBy(g => g.Item1.Item1)
            .ThenBy(g => g.Item1.Item2)
            .ThenBy(g => g.Item1.Item7)
            .Join(
                q.Currencies,
                g => g.Item1.Item1,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.Item1, g.Item2, c)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x =>
        {
            Assert.True(x.Item2 > 0);
            Assert.Equal(x.Item1.Item1, x.Item3.CurrencyId);
        });
    }

    [Fact]
    public async Task TupleArity8_Grouping_Throws()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                g => ValueTuple.Create(
                    g.Key,
                    g.Count(),
                    g.Sum(o => o.Total),
                    g.Min(o => o.Total),
                    g.Max(o => o.Total),
                    g.Count(o => o.Total > 50),
                    g.Max(o => o.AccountId),
                    g.Min(o => o.AccountId)))));

        Assert.Equal("ValueTuple arity > 7 not supported yet.", exception.Message);
    }

    [Fact]
    public async Task NestedTuple_Grouping_Select()
    {
        var rows = await Query(q => q.Orders
            .GroupBy(
                o => ValueTuple.Create(o.CurrencyId, o.AccountId),
                g => ValueTuple.Create(
                    ValueTuple.Create(g.Key.Item1, g.Key.Item2),
                    ValueTuple.Create(g.Count(), g.Sum(o => o.Total))))
            .Where(g => g.Item2.Item1 > 0)
            .OrderBy(g => g.Item1.Item1)
            .Select(g => ValueTuple.Create(g.Item1.Item1, g.Item1.Item2, g.Item2.Item1, g.Item2.Item2)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item3 > 0));
    }

    [Fact]
    public async Task ObjectContainingTuple_Grouping_Select()
    {
        var rows = await Query(q => q.Orders
            .GroupBy(
                o => o.CurrencyId,
                g => new
                {
                    Key = ValueTuple.Create(g.Key, g.Max(o => o.AccountId)),
                    Count = g.Count()
                })
            .Where(g => g.Count > 0)
            .OrderBy(g => g.Key.Item1)
            .Select(g => ValueTuple.Create(g.Key.Item1, g.Key.Item2, g.Count)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item3 > 0));
    }
}
