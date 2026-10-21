using QChain.Tests.TestBench;
using Microsoft.EntityFrameworkCore;
using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

using QChain;

namespace QChain.Tests.Queries;

public class SelectMany(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public void PlainCollectionSelector_FlattensInMemoryCollection()
    {
        var source = new[]
        {
            new[] { 1, 2 }, new[] { 3, 4 }
        }.AsQueryable().AsQuery();

        var items = source
            .SelectMany(x => x)
            .ToArray();

        Assert.Equal([1, 2, 3, 4], items);
    }

    [Fact]
    public async Task AfterGroupJoin_FlattensCollection()
    {
        Order[] orders = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .SelectMany(x => x.Item2)
                .OrderBy(o => o.OrderId));

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], orders.Select(o => o.OrderId));
    }

    [Fact]
    public async Task AfterGroupJoin_WithResultSelector()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .SelectMany(
                    x => x.Item2,
                    (x, o) => ValueTuple.Create(x.Item1.AccountId, o.OrderId))
                .OrderBy(x => x.Item2));

        Assert.NotEmpty(rows);
        Assert.Equal(7, rows.Length);
        Assert.All(rows, x => Assert.True(x.Item1 > 0));
    }

    [Fact]
    public async Task FlattenThenJoin()
    {
        (int orderId, int transactionId)[] rows = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .SelectMany(x => x.Item2)
                .Join(q.Transactions, o => o.OrderId, t => t.OrderId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .Select(x => ValueTuple.Create(x.Item1.OrderId, x.Item2.TransactionId))
                .OrderBy(x => x.Item2));

        Assert.Equal(await Query(q => q.Transactions.Select(t => t.TransactionId)), rows.Select(x => x.transactionId));
    }

    [Fact]
    public async Task Flattens_GroupJoin_Results()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .SelectMany(x => x.Item2)
                .OrderBy(o => o.OrderId));

        Assert.NotEmpty(rows);
        Assert.All(rows, o => Assert.True(o.AccountId > 0));
        Assert.Equal(rows.Length, rows.Select(o => o.OrderId).Distinct().Count());
    }

    [Fact]
    public async Task Flattens_GroupJoin_With_ResultSelector()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .SelectMany(
                    x => x.Item2,
                    (x, o) => ValueTuple.Create(x.Item1.AccountId, o.AccountId, o.OrderId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
        Assert.Equal(rows.Length, rows.Select(x => x.Item3).Distinct().Count());
    }

    [Fact]
    public async Task Can_Join_After_SelectMany()
    {
        var rows = await Query(q =>
            q.Accounts
                .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .SelectMany(x => x.Item2)
                .Join(q.Transactions, o => o.OrderId, t => t.OrderId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .Select(x => ValueTuple.Create(x.Item1.OrderId, x.Item2.OrderId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }
}
