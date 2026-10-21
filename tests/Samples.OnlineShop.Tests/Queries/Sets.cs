using QChain;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class Sets(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
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
    public async Task IntersectBy_ThenGroupBy()
    {
        var items = await Query(q =>
            q.Orders
                .IntersectBy([1, 2], o => o.AccountId)
                .GroupBy(
                    o => o.AccountId,
                    g => ValueTuple.Create(g.Key, g.Count()))
                .OrderBy(x => x.Item1));

        Assert.Equal([1, 2], items.Select(x => x.Item1));
        Assert.All(items, x => Assert.True(x.Item2 > 0));
    }
}
