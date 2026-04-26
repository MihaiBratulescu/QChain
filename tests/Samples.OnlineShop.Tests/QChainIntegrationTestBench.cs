using QChain;
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
    public async Task OnTable()
    {
        IGrouping<string?, Account>[] result = await Query(q =>
            q.Accounts.GroupBy1(a => a.Name));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, a => Assert.Equal(q.Key, a.Name)));
    }

    [Fact]
    public async Task WithElementSelector()
    {
        var result = await Query(q =>
            q.Accounts
             .GroupBy2(a => a.Name, a => new { a.Name, a.IsActive }));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q, a => Assert.Equal(q.Key, a.Name)));
    }

    [Fact]
    public async Task WithProjection()
    {
        (string? name, int count)[] result = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => ValueTuple.Create(g.Key, g.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.count > 0));
    }

    [Fact]
    public async Task WithJoin_Lambda()
    {
        var test = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => new { g.Key, total = g.Count() })
             .Join(q.Accounts, g => g.Key, a => a.Name));
    }

    [Fact]
    public async Task WithJoin_Tuple()
    {
        var test = await Query(q =>
            q.Accounts
             .GroupBy3(a => a.Name, g => ValueTuple.Create(g.Key, g.Count()))
             .Join(q.Accounts, g => g.Item1, a => a.Name));
    }
}
