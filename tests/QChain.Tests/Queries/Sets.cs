using QChain.Tests.TestBench;
using Microsoft.EntityFrameworkCore;
using QChain;
using Xunit.Abstractions;

namespace QChain.Tests.Queries;

public class Sets(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
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
        IQueryable<(int accountId, string? email)> active = _fixture.db.Accounts
            .Where(a => a.IsActive)
            .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        IQueryable<(int accountId, string? email)> inactive = _fixture.db.Accounts
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
            .Union(q.Accounts.Where(a => a.AccountId <= 7))
            .Join(q.Orders,
                a => a.AccountId,
                o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow)));

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
    public async Task Concat_Tuple()
    {
        IQueryable<(int accountId, string? email)> active =
            _fixture.db.Accounts
                .Where(a => a.IsActive)
                .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        IQueryable<(int accountId, string? email)> inactive =
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
                .GroupBy(a => a.IsActive)
                .Select(g => ValueTuple.Create(g.Key, g.Count()))
                .OrderBy(x => x.Item1));

        Assert.Equal([false, true], items.Select(x => x.Item1));
        Assert.Equal([2, 3], items.Select(x => x.Item2));
    }
}
