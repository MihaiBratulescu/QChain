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

public class GroupBy(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task OnTable()
    {
        (string? name, IEnumerable<Account> accounts)[] result = await Query(q =>
            q.Accounts.GroupBy(a => a.Name));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q.accounts, a => Assert.Equal(q.name, a.Name)));
    }

    [Fact]
    public async Task OnTuple()
    {
        (string? name, IEnumerable<(string? name, bool isActive)> accounts)[] result = await Query(q =>
            q.Accounts.Map(a => ValueTuple.Create(a.Name, a.IsActive)).GroupBy(a => a.Item1));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q.accounts, a => Assert.Equal(q.name, a.name)));
    }

    [Fact]
    public async Task TupleKey()
    {
        ((string? name, bool isActive) key, IEnumerable<Account> accounts)[] result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Name, a.IsActive)));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q.accounts, a => Assert.Equal(q.key.name, a.Name)));
    }

    [Fact]
    public async Task WithProjection()
    {
        //var test = await Query(q =>
        //    q.Accounts.GroupBy(a => a.Name, a => ValueTuple.Create(a.Key, a.Count()))
        //    .Join(q.Accounts, g => g.Item1, a => a.Name));

        (string? name, int count)[] result = await Query(q =>
            q.Accounts.GroupBy(a => a.Name, a => ValueTuple.Create(a.Key, a.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.count > 0) );
    }
}
