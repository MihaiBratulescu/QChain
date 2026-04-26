using QChain;
using QChain.EntityFrameworkCore;
using Samples.OnlineShop.Database;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests;

public class QChainIntegrationTestBench(SqliteFixture fixture) : IClassFixture<SqliteFixture>
{
    protected readonly SqliteFixture _fixture = fixture;

    protected Task<T[]> Query<T>(Func<IUnitOfWork, IQuery<T>> query) =>
        _fixture.db.Query(query).ToArrayAsync(default);
}

public class GroupJoin(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task _1_OnTable()
    {
        (string? name, int count)[] result = await Query(q =>
            q.Accounts
             //Tuple not supported
             .GroupBy1(a => a.Name)
             //Join not supported for IGrouping<,>
             .Map(g => ValueTuple.Create(g.Key, g.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.count > 0));
    }

    [Fact]
    public async Task _3_WithProjection()
    {
        (string? name, int count)[] result = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => ValueTuple.Create(g.Key, g.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.count > 0));
    }

    [Fact]
    public async Task _3_WithJoin_Lambda()
    {
        var test = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => new { g.Key, total = g.Count() })
             .Join(q.Accounts, g => g.Key, a => a.Name));
    }
}
