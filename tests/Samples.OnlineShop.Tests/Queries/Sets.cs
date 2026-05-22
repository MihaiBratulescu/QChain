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

        Assert.NotEmpty(concat);
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
    public async Task ExceptBy()
    {
        var items = await Query(q =>
            q.Accounts
                .ExceptBy([1, 2, 3], a => a.AccountId)
                .OrderBy(a => a.AccountId));

        Assert.DoesNotContain(items, a => a.AccountId is 1 or 2 or 3);
    }

    [Fact]
    public async Task Intersect()
    {
        var items = await Query(q =>
            q.Accounts
                .Intersect(q.Accounts.Where(a => a.IsActive))
                .OrderBy(a => a.AccountId));

        Assert.Equal([1, 2, 4, 6, 7], items.Select(a => a.AccountId));
        Assert.All(items, a => Assert.True(a.IsActive));
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
}
