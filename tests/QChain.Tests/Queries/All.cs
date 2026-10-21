using QChain.Tests.TestBench;
using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace QChain.Tests.Queries;

public class All(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task Execute2()
    {
        var shapedJoin = await Query(db => db.Accounts
            .GroupJoin(db.Orders.Select(o => new { o.OrderId, o.AccountId }), a => a.AccountId, o => o.AccountId,
                    (a, o) => new { a, o })
            .Where(x => x.a.AccountId < 100));
    }

    [Fact]
    public async Task Execute6()
    {
        (Account, IEnumerable<(int orderId, int accountId)>)[] shapedRight = await Query(db => db.Accounts
                .GroupJoin(db.Orders.Select(o => ValueTuple.Create(o.OrderId, o.AccountId)), a => a.AccountId, o => o.Item1, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow)));

    }

    [Fact]
    public async Task Execute7()
    {
        (Order o, Transaction tx, Currency c)[] nested = await Query(db =>
        {
            IQueryable<(Order order, Transaction transaction)> subquery = db.Orders
                .Join(db.Transactions, o => o.OrderId, t => t.OrderId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow));

            return db.Accounts.GroupJoin(subquery, a => a.AccountId, o => o.order.AccountId,
                (a, j) => ValueTuple.Create(a, j))
                .Where(x => x.Item1.AccountId < 100)
                .SelectMany(x => x.Item2)
                .Join(db.Currencies, x => x.order.CurrencyId, c => c.CurrencyId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .Select(x => ValueTuple.Create(x.Item1.order, x.Item1.transaction, x.Item2));
        });
    }

    [Fact]
    public async Task Execute8()
    {
        (int accountId, int orderId)[] distinct = await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .Select(a => ValueTuple.Create(a.Item2.AccountId, a.Item1.AccountId))
                .Distinct());
    }

    [Fact]
    public async Task Execute9()
    {
        (int, Order)[] distinct2 = await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .Select(a => a.Item2.AccountId)
                .Distinct()
                .Join(db.Orders, x => x, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow)));
    }
}
