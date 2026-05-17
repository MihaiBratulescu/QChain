using QChain;

namespace Samples.OnlineShop.Tests.Queries;

public class Sets(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
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
            .Map(a => a.AccountId)
            .ToArrayAsync(default);

        Assert.NotEmpty(union);
    }

    [Fact]
    public async Task Union_Tuple()
    {
        IQuery<(int accountId, string? email)> active = _fixture.db.Accounts
            .Where(a => a.IsActive)
            .Map(a => ValueTuple.Create(a.AccountId, a.Email));

        IQuery<(int accountId, string? email)> inactive = _fixture.db.Accounts
            .Where(a => !a.IsActive)
            .Map(a => ValueTuple.Create(a.AccountId, a.Email));

        var union = await active
            .Union(inactive)
            .OrderBy(a => a.accountId)
            .Map(a => a.email)
            .ToArrayAsync(default);

        Assert.NotEmpty(union);
    }

    [Fact]
    public async Task Concat()
    {
        var active = _fixture.db.Accounts
            .Where(a => a.IsActive)
            .Map(a => a.AccountId);

        var inactive = _fixture.db.Accounts
            .Where(a => !a.IsActive)
            .Map(a => a.AccountId);

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
}
