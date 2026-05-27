using QChain;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class Sets(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Union()
    {
        var active = _fixture.db.Accounts
            .Where(a => a.IsActive);

        var inactive = _fixture.db.Accounts
            .Where(a => !a.IsActive);

        var union = await active
            .Union(inactive)
            .OrderBy(a => a.AccountId)
            .Select(a => a.AccountId)
            .ToArrayAsync(default);

        Assert.NotEmpty(union);
        Assert.Equal(_fixture.db.Accounts.Select(a => a.AccountId).ToArray(), union);
    }

    [Fact]
    public async Task Union_Tuple()
    {
        IQuery<(int accountId, string? email)> active = _fixture.db.Accounts
            .Where(a => a.IsActive)
            .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        IQuery<(int accountId, string? email)> inactive = _fixture.db.Accounts
            .Where(a => !a.IsActive)
            .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        var union = await active
            .Union(inactive)
            .OrderBy(a => a.accountId)
            .Select(a => a.email)
            .ToArrayAsync(default);

        Assert.NotEmpty(union);
        Assert.Equal(_fixture.db.Accounts.Count(), union.Length);
    }

    [Fact]
    public async Task Union_Then_Join()
    {
        var items = await Query(q =>
        q.Accounts
            .Where(a => a.AccountId <= 5)
            .Union(q.Accounts.Where(a => a.AccountId  <= 7))
            .Join(q.Orders,
                a => a.AccountId,
                o => o.AccountId));

        Assert.NotEmpty(items);
        Assert.All(items,
            x => Assert.Equal(
                x.Item1.AccountId,
                x.Item2.AccountId));
    }

    [Fact]
    public async Task Union_Then_Where_Select()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 2)
                .Select(a => ValueTuple.Create(a.AccountId, a.Email))
                .Union(q.Accounts
                    .Where(a => a.AccountId >= 3 && a.AccountId <= 4)
                    .Select(a => ValueTuple.Create(a.AccountId, a.Email)))
                .Where(x => x.Item1 > 2)
                .Select(x => x.Item1)
                .OrderBy(x => x));

        Assert.Equal([3, 4], items);
    }

    [Fact]
    public async Task Union_Then_GroupBy()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 3)
                .Select(a => new { a.IsActive, a.AccountId })
                .Union(q.Accounts
                    .Where(a => a.AccountId >= 4 && a.AccountId <= 5)
                    .Select(a => new { a.IsActive, a.AccountId }))
                .GroupBy(
                    a => a.IsActive,
                    g => ValueTuple.Create(g.Key, g.Count()))
                .OrderBy(x => x.Item1));

        Assert.Equal([false, true], items.Select(x => x.Item1));
        Assert.Equal([2, 3], items.Select(x => x.Item2));
    }

    [Fact]
    public async Task Concat()
    {
        var active = _fixture.db.Accounts
            .Where(a => a.IsActive)
            .Select(a => a.AccountId);

        var inactive = _fixture.db.Accounts
            .Where(a => !a.IsActive)
            .Select(a => a.AccountId);

        var concat = await active
            .Concat(inactive)
            .ToArrayAsync(default);

        var expectedCount = await _fixture.db.Accounts.CountAsync();

        Assert.Equal(expectedCount, concat.Length);
    }

    [Fact]
    public async Task Except()
    {
        var all = _fixture.db.Accounts;

        var inactive = _fixture.db.Accounts
            .Where(a => !a.IsActive);

        var except = await all
            .Except(inactive)
            .ToArrayAsync(default);

        Assert.NotEmpty(except);
        Assert.All(except, a => Assert.True(a.IsActive));
    }

    [Fact]
    public async Task Except_Then_Join()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 5)
                .Select(a => new { a.AccountId, a.Email })
                .Except(q.Accounts
                    .Where(a => a.AccountId <= 2)
                    .Select(a => new { a.AccountId, a.Email }))
                .Join(
                    q.Orders,
                    a => a.AccountId,
                    o => o.AccountId,
                    (a, o) => ValueTuple.Create(a.AccountId, o.AccountId))
                .OrderBy(x => x.Item1));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.Equal(x.Item1, x.Item2));
        Assert.DoesNotContain(items, x => x.Item1 <= 2);
    }

    [Fact]
    public async Task ExceptBy()
    {
        var items = await Query(q =>
            q.Accounts
                .ExceptBy([1, 2, 3], a => a.AccountId)
                .OrderBy(a => a.AccountId));

        Assert.DoesNotContain(items, a => a.AccountId is 1 or 2 or 3);
    }

    [Fact]
    public async Task ExceptBy_AfterTupleProjection()
    {
        var items = await Query(q =>
            q.Accounts
                .Select(a => ValueTuple.Create(a.AccountId, a.Email))
                .ExceptBy([1, 2, 3], x => x.Item1)
                .OrderBy(x => x.Item1));

        Assert.Equal([4, 5, 6, 7], items.Select(x => x.Item1));
    }

    [Fact]
    public async Task ExceptBy_Filters_DuplicateKeys()
    {
        var items = await Query(q =>
            q.Orders
                .OrderBy(o => o.OrderId)
                .ExceptBy([3, 5], o => o.AccountId));

        Assert.Equal([1, 2], items.Select(o => o.AccountId).Distinct());
        Assert.DoesNotContain(items, o => o.AccountId is 3 or 5);
    }

    [Fact]
    public void ExceptBy_Translates_AsServerKeyFilter()
    {
        var query = _fixture.db.Orders
            .OrderBy(o => o.OrderId)
            .ExceptBy([3, 5], o => o.AccountId);

        var sql = query.ToQueryString().ToUpperInvariant();

        Assert.Contains("WHERE", sql);
        Assert.Contains("ACCOUNTID", sql);
        Assert.Contains("NOT", sql);
        Assert.True(sql.Contains(" IN ") || sql.Contains("JSON_EACH") || sql.Contains("EXISTS"), sql);
    }

    [Fact]
    public async Task Intersect()
    {
        var items = await Query(q =>
            q.Accounts
                .Intersect(q.Accounts.Where(a => a.IsActive))
                .OrderBy(a => a.AccountId));

        Assert.NotEmpty(items);
        Assert.All(items, a => Assert.True(a.IsActive));
    }

    [Fact]
    public async Task Intersect_Then_Join()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 5)
                .Select(a => new { a.AccountId, a.Email })
                .Intersect(q.Accounts
                    .Where(a => a.AccountId >= 3)
                    .Select(a => new { a.AccountId, a.Email }))
                .Join(
                    q.Orders,
                    a => a.AccountId,
                    o => o.AccountId,
                    (a, o) => ValueTuple.Create(a.AccountId, o.AccountId))
                .OrderBy(x => x.Item1));

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.Equal(x.Item1, x.Item2));
        Assert.All(items, x => Assert.True(x.Item1 >= 3));
    }

    [Fact]
    public async Task IntersectBy()
    {
        var items = await Query(q =>
            q.Accounts
                .IntersectBy([1, 2, 3], a => a.AccountId)
                .OrderBy(a => a.AccountId));

        Assert.Equal([1, 2, 3], items.Select(a => a.AccountId));
    }

    [Fact]
    public async Task IntersectBy_AfterObjectProjection()
    {
        var items = await Query(q =>
            q.Accounts
                .Select(a => new { Id = a.AccountId, a.Email })
                .IntersectBy([1, 2, 3], x => x.Id)
                .OrderBy(x => x.Id));

        Assert.Equal([1, 2, 3], items.Select(x => x.Id));
    }

    [Fact]
    public async Task IntersectBy_Filters_DuplicateKeys()
    {
        var items = await Query(q =>
            q.Orders
                .OrderBy(o => o.OrderId)
                .IntersectBy([1, 2], o => o.AccountId));

        Assert.Equal([1, 2], items.Select(o => o.AccountId).Distinct());
        Assert.All(items, o => Assert.True(o.AccountId is 1 or 2));
    }

    [Fact]
    public void IntersectBy_Translates_AsServerKeyFilter()
    {
        var query = _fixture.db.Orders
            .OrderBy(o => o.OrderId)
            .IntersectBy([1, 2], o => o.AccountId);

        var sql = query.ToQueryString().ToUpperInvariant();

        Assert.Contains("WHERE", sql);
        Assert.Contains("ACCOUNTID", sql);
        Assert.True(sql.Contains(" IN ") || sql.Contains("JSON_EACH") || sql.Contains("EXISTS"), sql);
    }

    [Fact]
    public async Task Concat_Tuple()
    {
        IQuery<(int accountId, string? email)> active =
            _fixture.db.Accounts
                .Where(a => a.IsActive)
                .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        IQuery<(int accountId, string? email)> inactive =
            _fixture.db.Accounts
                .Where(a => !a.IsActive)
                .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        var items = await active
            .Concat(inactive)
            .OrderBy(x => x.accountId)
            .ToArrayAsync();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], items.Select(x => x.accountId));
    }

    [Fact]
    public async Task Union_Preserves_RightProjectionShape()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 2)
                .Select(a => a.AccountId)
                .Union(q.Accounts
                    .Where(a => a.AccountId >= 3 && a.AccountId <= 4)
                    .Select(a => a.AccountId + 100))
                .OrderBy(x => x));

        Assert.Equal([1, 2, 103, 104], items);
    }

    [Fact]
    public async Task Concat_Preserves_RightProjectionShape()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 2)
                .Select(a => a.AccountId)
                .Concat(q.Accounts
                    .Where(a => a.AccountId >= 3 && a.AccountId <= 4)
                    .Select(a => a.AccountId + 100))
                .OrderBy(x => x));

        Assert.Equal([1, 2, 103, 104], items);
    }

    [Fact]
    public async Task Except_Compares_PublicProjectionShape()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 4)
                .Select(a => a.AccountId)
                .Except(q.Accounts
                    .Where(a => a.AccountId >= 3 && a.AccountId <= 4)
                    .Select(a => a.AccountId - 2))
                .OrderBy(x => x));

        Assert.Equal([3, 4], items);
    }

    [Fact]
    public async Task Intersect_Compares_PublicProjectionShape()
    {
        var items = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 4)
                .Select(a => a.AccountId)
                .Intersect(q.Accounts
                    .Where(a => a.AccountId >= 3 && a.AccountId <= 4)
                    .Select(a => a.AccountId - 2))
                .OrderBy(x => x));

        Assert.Equal([1, 2], items);
    }
}
