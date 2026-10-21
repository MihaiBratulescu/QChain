using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class PagingAndSorting(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Page()
    {
        var ids = await Query(q =>
            q.Accounts
                .OrderBy(a => a.AccountId)
                .Page(1, 3)
                .Select(a => a.AccountId));

        Assert.Equal([4, 5, 6], ids);
    }

    [Fact]
    public async Task Page_AfterJoinProjection()
    {
        var rows = await Query(q =>
            q.Accounts
                .Join(q.Orders, a => a.AccountId, o => o.AccountId)
                .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.OrderId))
                .OrderBy(x => x.Item2)
                .Page(1, 2));

        Assert.Equal([3, 4], rows.Select(x => x.Item2));
    }
}
