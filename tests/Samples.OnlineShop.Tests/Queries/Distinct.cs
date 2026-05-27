using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class Distinct(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task EntityProjection()
    {
        var rows = await Query(q => q.Accounts
            .Distinct()
            .OrderBy(a => a.AccountId));

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], rows.Select(a => a.AccountId));
    }

    [Fact]
    public async Task ScalarProjection()
    {
        var rows = await Query(q => q.Orders
            .Select(o => o.AccountId)
            .Distinct()
            .OrderBy(id => id));

        Assert.Equal(rows, rows.Distinct());
        Assert.All(rows, id => Assert.True(id > 0));
    }

    [Fact]
    public async Task ObjectProjection()
    {
        var rows = await Query(q => q.Accounts
            .Select(a => new { a.AccountId, a.IsActive })
            .Distinct()
            .OrderBy(a => a.AccountId));

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], rows.Select(a => a.AccountId));
    }

    [Fact]
    public async Task TupleProjection()
    {
        var distinct = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.CurrencyId))
            .Distinct());

        Assert.NotEmpty(distinct);
        Assert.Equal(distinct, distinct.Distinct());
    }

    [Fact]
    public async Task TupleProjection_ThenWhere()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.CurrencyId))
            .Distinct()
            .Where(x => x.Item1 == 1)
            .OrderBy(x => x.Item2));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(1, x.Item1));
        Assert.Equal(rows, rows.Distinct());
    }

    [Fact]
    public async Task TupleProjection_ThenOrderBy()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.CurrencyId))
            .Distinct()
            .OrderByDescending(x => x.Item1)
            .ThenBy(x => x.Item2));

        Assert.NotEmpty(rows);
        Assert.Equal(
            rows.OrderByDescending(x => x.Item1).ThenBy(x => x.Item2),
            rows);
    }

    [Fact]
    public async Task TupleProjection_ThenJoin()
    {
        (int accountId, CurrencyType currencyId, int orderId)[] rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.CurrencyId))
            .Distinct()
            .Join(
                q.Orders,
                x => x.Item1,
                o => o.AccountId,
                (x, o) => ValueTuple.Create(x.Item1, x.Item2, o.OrderId))
            .OrderBy(x => x.Item3));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.accountId > 0));
    }
}
