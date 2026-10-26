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

    [Fact]
    public async Task Execute1()
    {
        (int id, bool isActive)[] accounts = await Query(db => db.Accounts
            .Where(a => true)
            .Where(a => a.IsActive)
            .Select(a => ValueTuple.Create(a.AccountId, a.IsActive))
            .Where(a => a.Item1 % 2 == 0)
            .OrderByDescending(a => a.Item1));
    }

    [Fact]
    public async Task Execute3()
    {
        (Account account, IEnumerable<Order> orders)[] accountWithOrders = await Query(db => db.Accounts
            .Where(a => a.IsActive)
            .GroupJoin(
                db.Orders.OrderByDescending(o => o.CreatedDate)
                    .Where(o => o.CreatedDate >= DateTime.UtcNow.AddMonths(-1)),
                a => a.AccountId,
                o => o.AccountId,
                (account, orders) => ValueTuple.Create(account, orders))
            .Where(x => x.Item1.CreatedDate.Date == DateTime.UtcNow.Date)
            .Where(x => x.Item1.AccountId < 100)
            .OrderByDescending(a => a.Item1.AccountId)
            .ThenBy(a => a.Item1.IsActive));
    }

    [Fact]
    public async Task Execute4()
    {
        (Order order, IEnumerable<Transaction> transactions)[] ordersWithTransactions = await Query(db => db.Orders
            .Where(o => new[] { CurrencyType.EUR, CurrencyType.USD }.Contains(o.CurrencyId))
            .GroupJoin(db.Transactions, o => o.OrderId, t => t.TransactionId,
                (order, transactions) => ValueTuple.Create(order, transactions)));
    }

    [Fact]
    public async Task Execute5()
    {
        (Account a, IEnumerable<Order> o, Transaction t, Currency c)[] all = await Query(db => db.Accounts
            .GroupJoin(db.Orders.Where(o => o.CreatedDate >= DateTime.UtcNow.AddMonths(-1)),
                a => a.AccountId, o => o.AccountId,
                (account, orders) => ValueTuple.Create(account, orders))
            .Join(db.Transactions.Where(t => t.Status == TransactionStatus.Settled), j => j.Item1.AccountId, t => t.OrderId,
                    (j, t) => new { j.Item1, j.Item2, tx = t })
             .Join(db.Currencies, j => j.Item2.Select(o => o.CurrencyId).FirstOrDefault(), c => c.CurrencyId,
                    (j, c) => ValueTuple.Create(j.Item1, j.Item2, j.tx, c))
             .Where(t => t.Item1.AccountId < 100));
    }

    [Fact]
    public async Task Execute10()
    {
        (CurrencyType ct, int accountId, (Order, Account)[])[] group =
            await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .GroupBy(a => ValueTuple.Create(a.Item1.CurrencyId, a.Item2.AccountId))
                .Select(j => ValueTuple.Create(j.Key.Item1, j.Key.Item2, j.ToArray())));
    }

    [Fact]
    public async Task Execute11()
    {
        (CurrencyType, int activeCount, decimal sum, Currency currency)[] group2 = await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
                .Select(x => ValueTuple.Create(x.Item1, x.Item2))
                .GroupBy(a => new { a.Item1.CurrencyId, a.Item2.AccountId })
            .Select(g => new
            {
                g.Key,

                // basic aggregates
                count = g.Count(),
                longCount = g.LongCount(),
                maxId = g.Max(x => x.Item1.AccountId),
                minId = g.Min(x => x.Item1.AccountId),
                sum = g.Sum(x => x.Item1.Total),
                avg = g.Average(x => x.Item1.Total),

                // predicates
                anyActive = g.Any(x => x.Item2.IsActive),
                allActive = g.All(x => x.Item2.IsActive),
                any = g.Any(),

                // conditional aggregates
                activeCount = g.Count(x => x.Item2.IsActive),
                inactiveCount = g.Count(x => !x.Item2.IsActive),

                // nullable / coalesce paths
                maxNullable = g.Max(x => (int?)x.Item1.AccountId),
                sumNullable = g.Sum(x => (decimal?)x.Item1.Total) ?? 0,

                // distinct-ish scalar projection
                distinctAccounts = g.Select(x => x.Item1.AccountId).Distinct().Count(),

                // first/ordered element patterns
                firstAccountId = g
                        .OrderBy(x => x.Item1.AccountId)
                        .Select(x => x.Item1.AccountId)
                        .FirstOrDefault(),

                // filtered aggregate
                activeMaxId = g
                        .Where(x => x.Item2.IsActive)
                        .Max(x => (int?)x.Item1.AccountId),

                // composite boolean
                hasPositiveAmount = g.Any(x => x.Item1.Total > 0),

                maxFromAnon = g
                        .Select(x => new { Id = x.Item1.AccountId })
                        .Max(x => x.Id),

                filteredSum = g
                        .Where(x => x.Item2.IsActive)
                        .Sum(x => x.Item1.Total),

                complexAny = g.Any(x => x.Item2.IsActive && x.Item1.Total > 100),

                conditionalSum = g.Sum(x => x.Item2.IsActive ? x.Item1.Total : 0),

                safeMax = g.Max(x => (int?)x.Item1.AccountId) ?? -1,

                avgOfActive = g
                        .Where(x => x.Item2.IsActive)
                        .Average(x => (decimal?)x.Item1.Total),

                MaxReuse = g.Select(x => x.Item1.AccountId).Max(),
                MinReuse = g.Select(x => x.Item1.AccountId).Min(),
            })
                .Join(db.Currencies, g => g.Key.CurrencyId, c => c.CurrencyId,
                    (g, c) => ValueTuple.Create(g.Key.CurrencyId, g.activeCount, g.filteredSum, c)));
    }
}
