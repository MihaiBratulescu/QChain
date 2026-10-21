using QChain.Tests.TestBench;
using Microsoft.EntityFrameworkCore;
using QChain;
using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace QChain.Tests.Queries;

public class GroupJoin(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task Preserves_Outer_Items()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .Select(x => ValueTuple.Create(
                    x.Item1.AccountId,
                    x.Item2.Count())));

        var accountCount = await _fixture.db.Accounts.CountAsync();

        Assert.Equal(accountCount, rows.Length);
        Assert.All(rows, x => Assert.True(x.Item1 > 0));
        Assert.All(rows, x => Assert.True(x.Item2 >= 0));
    }

    [Fact]
    public async Task Custom_ResultSelector_Preserves_Group()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(
                    q.Orders,
                    a => a.AccountId,
                    o => o.AccountId,
                    (a, orders) => ValueTuple.Create(
                        a.AccountId,
                        orders.Count()))
                .OrderBy(x => x.Item1));

        var accountCount = await _fixture.db.Accounts.CountAsync();

        Assert.Equal(accountCount, rows.Length);
        Assert.All(rows, x => Assert.True(x.Item1 > 0));
        Assert.All(rows, x => Assert.True(x.Item2 >= 0));
    }

    [Fact]
    public async Task GroupJoin_And_SelectMany_Are_Equivalent()
    {
        var flattened = await Query(q => q.Accounts
            .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
            .SelectMany(x => x.Item2)
            .Select(o => o.OrderId));

        var joined = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
            .Select(x => x.Item2.OrderId));

        Assert.Equal(
            joined.OrderBy(x => x),
            flattened.OrderBy(x => x));
    }
}
