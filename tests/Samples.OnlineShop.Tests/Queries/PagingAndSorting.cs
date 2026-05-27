using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class PagingAndSorting(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Skip()
    {
        var ids = await Query(q =>
            q.Accounts
                .OrderBy(a => a.AccountId)
                .Skip(2)
                .Select(a => a.AccountId));

        Assert.Equal([3, 4, 5, 6, 7], ids);
    }

    [Fact]
    public async Task Take()
    {
        var ids = await Query(q =>
            q.Accounts
                .OrderBy(a => a.AccountId)
                .Take(3)
                .Select(a => a.AccountId));

        Assert.Equal([1, 2, 3], ids);
    }

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
    public async Task Paging_AfterTupleProjection()
    {
        var rows = await Query(q =>
            q.Accounts
                .Select(a => ValueTuple.Create(a.AccountId, a.Email))
                .OrderBy(x => x.Item1)
                .Skip(1)
                .Take(3));

        Assert.Equal([2, 3, 4], rows.Select(x => x.Item1));
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

    [Fact]
    public async Task Reverse()
    {
        var ids = await Query(q =>
            q.Accounts
                .OrderBy(a => a.AccountId)
                .Reverse()
                .Select(a => a.AccountId));

        Assert.Equal([7, 6, 5, 4, 3, 2, 1], ids);
    }

    [Fact]
    public async Task ThenByDescending()
    {
        var rows = await Query(q =>
            q.Accounts
                .OrderBy(a => a.IsActive)
                .ThenByDescending(a => a.AccountId)
                .Select(a => ValueTuple.Create(a.IsActive, a.AccountId)));

        Assert.Equal(
            [
                ValueTuple.Create(false, 5),
                ValueTuple.Create(false, 3),
                ValueTuple.Create(true, 7),
                ValueTuple.Create(true, 6),
                ValueTuple.Create(true, 4),
                ValueTuple.Create(true, 2),
                ValueTuple.Create(true, 1),
            ],
            rows);
    }

    [Fact]
    public async Task ThenBy_AfterObjectProjection()
    {
        var rows = await Query(q =>
            q.Accounts
                .Select(a => new { a.IsActive, a.AccountId })
                .OrderBy(a => a.IsActive)
                .ThenBy(a => a.AccountId));

        Assert.Equal([3, 5, 1, 2, 4, 6, 7], rows.Select(x => x.AccountId));
    }
}
