using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests.Queries;

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
            q.Accounts
             .Map(a => ValueTuple.Create(a.Name, a.IsActive))
             .GroupBy(a => a.Item1));

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
    public async Task TupleKey_Projected()
    {
        ((string?, bool), int total)[] result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Name, a.IsActive),
                               g => ValueTuple.Create(g.Key, g.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.total > 0));
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

    [Fact]
    public async Task WithJoin()
    {
        var result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Name, a.IsActive),
                               g => new { g.Key, total = g.Count(), first = g.Min(a => a.AccountId) })
                      .GroupJoin(q.Orders, g => g.first, o => o.AccountId));

        Assert.NotEmpty(result);
    }
}
