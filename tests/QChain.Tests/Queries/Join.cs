using QChain.Tests.TestBench;
using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace QChain.Tests.Queries;

public class Join(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task TwoTables()
    {
        (Account account, Order order)[] result = await Query(q => q.Accounts
             .Join(q.Orders, 
                a => a.AccountId, o => o.AccountId, 
                (a, o) => ValueTuple.Create(a, o)));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.Equal(q.account.AccountId, q.order.AccountId));
    }

    [Fact]
    public async Task ThreeTables()
    {
        (Account account, Order order, Transaction transaction)[] result = await Query(q =>
            q.Accounts
             .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
             .Join(q.Transactions, j => j.Item2.OrderId, t => t.OrderId,
                (j, t) => ValueTuple.Create(j.Item1, j.Item2, t)));

        Assert.NotEmpty(result);
        Assert.All(result, q =>
        {
            Assert.Equal(q.account.AccountId, q.order.AccountId);
            Assert.Equal(q.order.OrderId, q.transaction.OrderId);
        });
    }

    [Fact]
    public async Task GroupJoin()
    {
        (Account account, IEnumerable<Order> orders)[] result = await Query(q =>
            q.Accounts
             .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId, 
                (a, o) => ValueTuple.Create(a, o)));

        Assert.NotEmpty(result);
        foreach (var (account, orders) in result)
        {
            Assert.All(orders, o => Assert.Equal(account.AccountId, o.AccountId));
        }
    }

    [Fact]
    public async Task Join_Join()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Join(q.Transactions, x => x.Item2.OrderId, t => t.OrderId, (j, t) => ValueTuple.Create(j, t))
            .Select(x => ValueTuple.Create(
                x.Item1.Item2.OrderId,
                x.Item2.OrderId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task Join_Join_Join()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Join(q.Transactions, x => x.Item2.OrderId, t => t.OrderId, (j, t) => ValueTuple.Create(j, t))
            .Join(q.Currencies, x => x.Item1.Item2.CurrencyId, c => c.CurrencyId, (j, t) => ValueTuple.Create(j, t))
            .Select(x => ValueTuple.Create(
                x.Item1.Item1.Item2.CurrencyId,
                x.Item2.CurrencyId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task Join_Then_Filter_On_Nested_Left()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Where(x => x.Item1.AccountId == x.Item2.AccountId)
            .Select(x => ValueTuple.Create(
                x.Item1.AccountId,
                x.Item2.AccountId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task Join_Join_Then_Filter_On_Nested_Key()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Join(q.Transactions, x => x.Item2.OrderId, t => t.OrderId, (j, t) => ValueTuple.Create(j, t))
            .Where(x => x.Item1.Item2.OrderId == x.Item2.OrderId)
            .Select(x => ValueTuple.Create(
                x.Item1.Item2.OrderId,
                x.Item2.OrderId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task Join_Join_Then_OrderBy_Nested_Key()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Join(q.Transactions, x => x.Item2.OrderId, t => t.OrderId, (j, t) => ValueTuple.Create(j, t))
            .OrderBy(x => x.Item1.Item2.OrderId)
            .Select(x => ValueTuple.Create(
                x.Item1.Item2.OrderId,
                x.Item2.OrderId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));

        Assert.Equal(
            rows.Select(x => x.Item1).OrderBy(x => x),
            rows.Select(x => x.Item1));
    }

    [Fact]
    public async Task Join_Join_Then_GroupBy_Nested_Key()
    {
        var joined = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Join(q.Transactions, x => x.Item2.OrderId, t => t.OrderId, (j, t) => ValueTuple.Create(j, t))
            .GroupBy(x => x.Item1.Item2.OrderId)
            .Select(g => g.Key));

        var direct = await Query(q => q.Transactions
            .GroupBy(t => t.OrderId)
            .Select(g => g.Key));

        Assert.Equal(
            direct.OrderBy(x => x),
            joined.OrderBy(x => x));
    }

    [Fact]
    public async Task Join_With_ResultSelector_Then_Join()
    {
        var rows = await Query(q => q.Accounts
            .Join(
                q.Orders,
                a => a.AccountId,
                o => o.AccountId,
                (a, o) => ValueTuple.Create(a.AccountId, o.OrderId, o.CurrencyId))
            .Join(q.Transactions, x => x.Item2, t => t.OrderId, (j, t) => ValueTuple.Create(j, t))
            .Select(x => ValueTuple.Create(
                x.Item1.Item2,
                x.Item2.OrderId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task Join_With_ResultSelector_Join_With_ResultSelector()
    {
        var rows = await Query(q => q.Accounts
            .Join(
                q.Orders,
                a => a.AccountId,
                o => o.AccountId,
                (a, o) => ValueTuple.Create(a.AccountId, o.OrderId, o.CurrencyId))
            .Join(
                q.Currencies,
                x => x.Item3,
                c => c.CurrencyId,
                (x, c) => ValueTuple.Create(
                    x.Item1,
                    x.Item2,
                    x.Item3,
                    c.CurrencyId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item3, x.Item4));
    }

    [Fact]
    public async Task Join_With_Projected_Right_Side()
    {
        var rows = await Query(q => q.Accounts
            .Join(
                q.Orders.Select(o => ValueTuple.Create(o.AccountId, o.OrderId)),
                a => a.AccountId,
                o => o.Item1,
                (a, o) => ValueTuple.Create(a.AccountId, o.Item1, o.Item2)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task Join_After_Distinct_With_Projected_Right_Side()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.CurrencyId))
            .Distinct()
            .Join(
                q.Orders.Select(o => new { o.AccountId, o.OrderId }),
                x => x.Item1,
                o => o.AccountId,
                (x, o) => ValueTuple.Create(x.Item1, o.AccountId, o.OrderId)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task GroupJoin_After_Distinct()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o))
            .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.CurrencyId))
            .Distinct()
            .GroupJoin(
                q.Orders,
                x => x.Item1,
                o => o.AccountId,
                (x, orders) => ValueTuple.Create(x.Item1, orders.Count()))
            .OrderBy(x => x.Item1));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item1 > 0));
        Assert.All(rows, x => Assert.True(x.Item2 > 0));
    }

    [Fact]
    public async Task Join_After_Union()
    {
        var rows = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 2)
                .Select(a => new { a.AccountId, Label = a.Email })
                .Union(q.Accounts
                    .Where(a => a.AccountId >= 3 && a.AccountId <= 4)
                    .Select(a => new { a.AccountId, Label = a.Email }))
                .Join(
                    q.Orders,
                    a => a.AccountId,
                    o => o.AccountId,
                    (a, o) => ValueTuple.Create(a.AccountId, o.AccountId, a.Label)));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Item1, x.Item2));
    }

    [Fact]
    public async Task GroupJoin_After_Union()
    {
        var rows = await Query(q =>
            q.Accounts
                .Where(a => a.AccountId <= 2)
                .Select(a => new { a.AccountId, Label = a.Email })
                .Union(q.Accounts
                    .Where(a => a.AccountId >= 3 && a.AccountId <= 4)
                    .Select(a => new { a.AccountId, Label = a.Email }))
                .GroupJoin(
                    q.Orders,
                    a => a.AccountId,
                    o => o.AccountId,
                    (a, orders) => ValueTuple.Create(a.AccountId, orders.Count()))
                .OrderBy(x => x.Item1));

        Assert.Equal([1, 2, 3, 4], rows.Select(x => x.Item1));
        Assert.All(rows, x => Assert.True(x.Item2 >= 0));
    }

    [Fact]
    public async Task Join_After_AnonymousProjection()
    {
        var rows = await Query(q => q.Accounts
            .Select(a => new
            {
                Id = a.AccountId,
                a.Email,
                Active = a.IsActive
            })
            .Join(
                q.Orders,
                a => a.Id,
                o => o.AccountId,
                (a, o) => new
                {
                    a.Id,
                    o.AccountId,
                    o.OrderId,
                    a.Active
                }));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(x.Id, x.AccountId));
    }

    [Fact]
    public async Task GroupJoin_After_AnonymousProjection()
    {
        var rows = await Query(q => q.Accounts
            .Select(a => new
            {
                Id = a.AccountId,
                a.Email,
                Active = a.IsActive
            })
            .GroupJoin(
                q.Orders,
                a => a.Id,
                o => o.AccountId,
                (a, orders) => new
                {
                    a.Id,
                    Count = orders.Count()
                })
            .OrderBy(x => x.Id));

        Assert.Equal(_fixture.db.Accounts.Select(a => a.AccountId).OrderBy(x => x).ToArray(), rows.Select(x => x.Id));
        Assert.All(rows, x => Assert.True(x.Count >= 0));
    }
}