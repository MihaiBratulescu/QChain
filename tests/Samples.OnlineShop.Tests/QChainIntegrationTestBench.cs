using QChain;
using Samples.OnlineShop.Database;

namespace Samples.OnlineShop.Tests;

public class QChainIntegrationTestBench(SqliteFixture fixture) : IClassFixture<SqliteFixture>
{
    protected readonly SqliteFixture _fixture = fixture;

    protected Task<T[]> Query<T>(Func<IUnitOfWork, IQuery<T>> query) =>
        _fixture.db.Query(query).ToArrayAsync(default);
}

public class Filtering(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task IsApplied()
    {
        var accounts = await Query(q => q.Accounts.Active());

        Assert.NotEmpty(accounts);
        Assert.All(accounts, a => Assert.True(a.IsActive));
    }

    [Fact]
    public async Task NotMatched_ReturnsEmpty()
    {
        var accounts = await Query(q => 
            q.Accounts.Before(DateTime.UtcNow.AddYears(-10)));

        Assert.Empty(accounts);
    }

    [Fact]
    public async Task AfterMap_UsesProjectedShape()
    {
        var rows = await Query(
            q => q.Accounts.Map(a => new
                {
                    Id = a.AccountId,
                    Active = a.IsActive
                })
                .Where(x => x.Active));

        Assert.NotEmpty(rows);
        Assert.All(rows, a => Assert.True(a.Active));
    }
}