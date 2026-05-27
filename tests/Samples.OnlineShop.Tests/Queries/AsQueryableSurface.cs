using QChain.Predicates;
using Samples.OnlineShop.DatabaseModels;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class AsQueryableSurface(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public void AsQueryable_AfterDefaultIfEmpty()
    {
        var rows = _fixture.db.Accounts
            .Where(a => a.AccountId > 100)
            .DefaultIfEmpty()
            .AsQueryable()
            .ToArray();

        Assert.Single(rows);
        Assert.Null(rows[0]);
    }

    [Fact]
    public void AsQueryable_AfterDistinct()
    {
        var rows = _fixture.db.Accounts
            .Select(a => a.Email)
            .Distinct()
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterWhereExpression()
    {
        var rows = _fixture.db.Accounts
            .Where(a => a.IsActive)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, a => Assert.True(a.IsActive));
    }

    [Fact]
    public void AsQueryable_AfterWherePredicate()
    {
        Expression<Func<Account, bool>> active = a => a.IsActive;
        Expression<Func<Account, bool>> evenId = a => a.AccountId % 2 == 0;

        var rows = _fixture.db.Accounts
            .Where(x => active.And(evenId))
            .AsQueryable()
            .ToArray();

        Assert.Equal([2, 4, 6], rows.Select(a => a.AccountId));
    }

    [Fact]
    public void AsQueryable_AfterRawGroupBy()
    {
        var rows = _fixture.db.Orders
            .GroupBy(o => o.AccountId)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, g => Assert.All(g, o => Assert.Equal(g.Key, o.AccountId)));
    }

    [Fact]
    public void AsQueryable_AfterRawGroupByElementSelector()
    {
        var rows = _fixture.db.Orders
            .GroupBy(o => o.AccountId, o => o.OrderId)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, g => Assert.All(g, orderId => Assert.True(orderId > 0)));
    }

    [Fact]
    public void AsQueryable_AfterProjectedGroupByGroupingSelector()
    {
        var rows = _fixture.db.Orders
            .GroupBy(o => o.AccountId, g => ValueTuple.Create(g.Key, g.Count()))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public void AsQueryable_AfterProjectedGroupByResultSelector()
    {
        var rows = _fixture.db.Orders
            .GroupBy(o => o.AccountId, (key, items) => ValueTuple.Create(key, items.Count()))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public void AsQueryable_AfterProjectedGroupByElementResultSelector()
    {
        var rows = _fixture.db.Orders
            .GroupBy(
                o => o.AccountId,
                o => o.Total,
                (key, items) => ValueTuple.Create(key, items.Sum()))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public void AsQueryable_AfterJoin()
    {
        var rows = _fixture.db.Accounts
            .Join(_fixture.db.Orders, a => a.AccountId, o => o.AccountId)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1.AccountId, x.Item2.AccountId));
    }

    [Fact]
    public void AsQueryable_AfterJoinResultSelector()
    {
        var rows = _fixture.db.Accounts
            .Join(
                _fixture.db.Orders,
                a => a.AccountId,
                o => o.AccountId,
                (a, o) => ValueTuple.Create(a.AccountId, o.OrderId))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item1 > 0 && x.Item2 > 0));
    }

    [Fact]
    public void AsQueryable_AfterGroupJoin()
    {
        var rows = _fixture.db.Accounts
            .GroupJoin(_fixture.db.Orders, a => a.AccountId, o => o.AccountId)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterGroupJoinResultSelector()
    {
        var rows = _fixture.db.Accounts
            .GroupJoin(
                _fixture.db.Orders,
                a => a.AccountId,
                o => o.AccountId,
                (a, orders) => ValueTuple.Create(a.AccountId, orders.Count()))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterLeftJoin()
    {
        var rows = _fixture.db.Accounts
            .LeftJoin(_fixture.db.Orders, a => a.AccountId, o => o.AccountId)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.Contains(rows, x => x.Item2 is null);
    }

    [Fact]
    public void AsQueryable_AfterLeftJoinResultSelector()
    {
        var rows = _fixture.db.Accounts
            .LeftJoin(
                _fixture.db.Orders,
                a => a.AccountId,
                o => o.AccountId,
                (a, o) => ValueTuple.Create(a.AccountId, o == null ? null : (int?)o.OrderId))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterRightJoin()
    {
        var rows = _fixture.db.Accounts
            .RightJoin(_fixture.db.Orders, a => a.AccountId, o => o.AccountId)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.NotNull(x.Item2));
    }

    [Fact]
    public void AsQueryable_AfterRightJoinResultSelector()
    {
        var rows = _fixture.db.Accounts
            .RightJoin(
                _fixture.db.Orders,
                a => a.AccountId,
                o => o.AccountId,
                (a, o) => ValueTuple.Create(a == null ? null : (int?)a.AccountId, o.OrderId))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterSkipTakePage()
    {
        var skipped = _fixture.db.Accounts.OrderBy(a => a.AccountId).Skip(1).AsQueryable().ToArray();
        var taken = _fixture.db.Accounts.OrderBy(a => a.AccountId).Take(2).AsQueryable().ToArray();
        var paged = _fixture.db.Accounts.OrderBy(a => a.AccountId).Page(1, 2).AsQueryable().ToArray();

        Assert.NotEmpty(skipped);
        Assert.Equal(2, taken.Length);
        Assert.Equal([3, 4], paged.Select(a => a.AccountId));
    }

    [Fact]
    public void AsQueryable_AfterSelect()
    {
        var rows = _fixture.db.Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.Email))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterSelectManyCollectionSelector()
    {
        var rows = _fixture.db.Accounts
            .GroupJoin(_fixture.db.Orders, a => a.AccountId, o => o.AccountId)
            .SelectMany(x => x.Item2)
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterSelectManyResultSelector()
    {
        var rows = _fixture.db.Accounts
            .GroupJoin(_fixture.db.Orders, a => a.AccountId, o => o.AccountId)
            .SelectMany(
                x => x.Item2,
                (x, order) => ValueTuple.Create(x.Item1.AccountId, order.OrderId))
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }

    [Fact]
    public void AsQueryable_AfterSets()
    {
        var active = _fixture.db.Accounts.Where(a => a.IsActive);
        var inactive = _fixture.db.Accounts.Where(a => !a.IsActive);

        Assert.NotEmpty(active.Union(inactive).AsQueryable().ToArray());
        Assert.NotEmpty(active.Concat(inactive).AsQueryable().ToArray());
        Assert.NotEmpty(_fixture.db.Accounts.Except(inactive).AsQueryable().ToArray());
        Assert.NotEmpty(_fixture.db.Accounts.Intersect(active).AsQueryable().ToArray());
    }

    [Fact]
    public void AsQueryable_AfterSetBy()
    {
        var except = _fixture.db.Accounts
            .ExceptBy([1, 2, 3], a => a.AccountId)
            .AsQueryable()
            .ToArray();
        var intersect = _fixture.db.Accounts
            .IntersectBy([1, 2, 3], a => a.AccountId)
            .AsQueryable()
            .ToArray();

        Assert.Equal([4, 5, 6, 7], except.Select(a => a.AccountId));
        Assert.Equal([1, 2, 3], intersect.Select(a => a.AccountId));
    }

    [Fact]
    public void AsQueryable_AfterSorting()
    {
        var rows = _fixture.db.Accounts
            .OrderByDescending(a => a.IsActive)
            .ThenBy(a => a.Email)
            .ThenByDescending(a => a.AccountId)
            .Reverse()
            .AsQueryable()
            .ToArray();

        Assert.NotEmpty(rows);
    }
}
