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
    public async Task RawGroup_Where_OnAggregate()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .Where(g => g.Count() > 1)
            .Select(g => ValueTuple.Create(g.Key, g.Count()))
            .OrderBy(x => x.Item1));

        Assert.Equal([1, 2], result.Select(x => x.Item1));
        Assert.All(result, x => Assert.True(x.Item2 > 1));
    }

    [Fact]
    public async Task RawGroup_OrderBy_OnKey()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .OrderByDescending(g => g.Key)
            .Select(g => g.Key));

        Assert.Equal([5, 3, 2, 1], result);
    }

    [Fact]
    public async Task RawGroup_OrderBy_OnTupleKey()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => ValueTuple.Create(o.CurrencyId, o.AccountId))
            .OrderBy(g => g.Key.Item1)
            .ThenBy(g => g.Key.Item2)
            .Select(g => ValueTuple.Create(g.Key.Item1, g.Key.Item2)));

        Assert.Equal(
            result.OrderBy(x => x.Item1).ThenBy(x => x.Item2),
            result);
    }

    [Fact]
    public async Task RawGroup_Where_OnTupleKeyMember()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => ValueTuple.Create(o.CurrencyId, o.AccountId))
            .Where(g => g.Key.Item1 == CurrencyType.EUR)
            .Select(g => ValueTuple.Create(g.Key.Item1, g.Key.Item2))
            .OrderBy(x => x.Item2));

        Assert.Equal(
            [ValueTuple.Create(CurrencyType.EUR, 1), ValueTuple.Create(CurrencyType.EUR, 2)],
            result);
    }

    [Fact]
    public async Task RawGroup_Where_OnElementAggregate()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .Where(g => g.Sum(o => o.Total) > 250)
            .Select(g => ValueTuple.Create(g.Key, g.Sum(o => o.Total)))
            .OrderBy(x => x.Item1));

        Assert.Equal([1, 3], result.Select(x => x.Item1));
        Assert.All(result, x => Assert.True(x.Item2 > 250));
    }

    [Fact]
    public async Task RawGroup_OrderBy_OnAggregate()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => ValueTuple.Create(g.Key, g.Count())));

        Assert.Equal([1, 2, 3, 5], result.Select(x => x.Item1));
        Assert.Equal([3, 2, 1, 1], result.Select(x => x.Item2));
    }

    [Fact]
    public async Task RawGroup_Filter_ThenMaterializeItems()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key)
            .Select(g => ValueTuple.Create(g.Key, g.ToArray())));

        Assert.Equal([1, 2], result.Select(x => x.Item1));
        Assert.All(result, x => Assert.True(x.Item2.Length > 1));
    }

    [Fact]
    public async Task RawGroup_Order_Page_Select()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .OrderBy(g => g.Key)
            .Skip(1)
            .Take(2)
            .Select(g => ValueTuple.Create(g.Key, g.Count())));

        Assert.Equal([2, 3], result.Select(x => x.Item1));
    }

    [Fact]
    public async Task RawGroup_ThenByDescending()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.CurrencyId)
            .OrderBy(g => g.Count())
            .ThenByDescending(g => g.Key)
            .Select(g => ValueTuple.Create(g.Key, g.Count())));

        Assert.Equal(
            result.OrderBy(x => x.Item2).ThenByDescending(x => x.Item1),
            result);
    }

    [Fact]
    public async Task RawGroup_Reverse()
    {
        var efQuery = _fixture.db.Orders
            .AsQueryable()
            .GroupBy(o => o.AccountId)
            .OrderBy(g => g.Key)
            .Reverse()
            .Select(g => g.Key);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToArrayAsync(efQuery));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .OrderBy(g => g.Key)
            .Reverse()
            .Select(g => g.Key)));
    }

    [Fact]
    public async Task RawGroup_Distinct_AfterSelect()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.AccountId)
            .Select(g => ValueTuple.Create(g.Key, g.Count()))
            .Distinct()
            .OrderBy(x => x.Item1));

        Assert.Equal([1, 2, 3, 5], result.Select(x => x.Item1));
        Assert.Equal(result, result.Distinct());
    }

    [Fact]
    public async Task RawGroup_Filter_Project_ThenJoin()
    {
        var result = await Query(q => q.Orders
            .GroupBy(o => o.CurrencyId)
            .Where(g => g.Count() > 1)
            .Select(g => ValueTuple.Create(g.Key, g.Count()))
            .Join(
                q.Currencies,
                g => g.Item1,
                c => c.CurrencyId,
                (g, c) => ValueTuple.Create(g.Item1, g.Item2, c.Symbol))
            .OrderBy(x => x.Item1));

        Assert.Equal([CurrencyType.EUR, CurrencyType.USD, CurrencyType.BTC], result.Select(x => x.Item1));
        Assert.All(result, x => Assert.True(x.Item2 > 1));
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

    [Fact]
    public async Task TwoJoins_ThenGroupBy_TupleKey_Aggregate()
    {
        var result = await Query(q => q.Orders
            .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
            .Join(q.Transactions, x => x.Item1.OrderId, t => t.OrderId)
            .GroupBy(
                x => ValueTuple.Create(
                    x.Item1.Item1.CurrencyId,
                    x.Item1.Item2.IsActive),
                g => new
                {
                    g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.Item1.Item1.Total)
                }));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Project_ThenGroupBy_Aggregate()
    {
        var result = await Query(q => q.Orders
            .Select(o => new { o.AccountId, o.CurrencyId, o.Total })
            .GroupBy(
                x => x.AccountId,
                g => new
                {
                    AccountId = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.Total)
                }));

        Assert.NotEmpty(result);
    }
}
