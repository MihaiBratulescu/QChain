using QChain;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests;

public class All(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task Execute()
    {
        (int id, bool isActive) [] accounts = await Query(db => db.Accounts
            .FromEurope()
            .Active()
            .Map(a => ValueTuple.Create(a.AccountId, a.IsActive))
            .Where(a => a.Item1 % 2 == 0)
            .OrderByDescending(a => a.Item1));

        var shapedJoin = await Query(db => db.Accounts
            .GroupJoin(db.Orders.Map(o => new { o.OrderId, o.AccountId }), a => a.AccountId, o => o.AccountId,
                    (a, o) => new { a, o })
            .Where(x => x.a.AccountId < 100));

        (Account account, IEnumerable<Order> orders)[] accountWithOrders = await Query(db => db.Accounts
            .Active()
            .WithOrders(db.Orders.NewestFirst()
                                 .InLastMonth())
            .Where(x => x.account.CreatedDate.Date == DateTime.UtcNow.Date)
            .Where(x => x.account.AccountId < 100)
            .OrderByDescending(a => a.account.AccountId)
            .ThenBy(a => a.account.IsActive));

        (Order order, IEnumerable<Transaction> transactions)[] ordersWithTransactions = await Query(db => db.Orders
                .WithCurrencies(CurrencyType.EUR, CurrencyType.USD)
                .WithTransactions(db.Transactions));

        (Account a, IEnumerable<Order> o, Transaction t, Currency c)[] all = await Query(db => db.Accounts
            .WithOrders(db.Orders.InLastMonth())
            .Join(db.Transactions.Settled(), j => j.account.AccountId, t => t.OrderId,
                    (j, t) => new { j.account, j.orders, tx = t })
             .Join(db.Currencies, j => j.orders.Select(o => o.CurrencyId).FirstOrDefault(), c => c.CurrencyId,
                    (j, c) => ValueTuple.Create(j.account, j.orders, j.tx, c))
             .Where(t => t.Item1.AccountId < 100));

        (Account, IEnumerable<(int orderId, int accountId)>)[] shapedRight = await Query(db => db.Accounts
                .GroupJoin(db.Orders.Map(o => ValueTuple.Create(o.OrderId, o.AccountId)), a => a.AccountId, o => o.Item1));

        (Order o, Transaction tx, Currency c)[] nested = await Query(db =>
        {
            IQuery<(Order order, Transaction transaction)> subquery = db.Orders
                .Join(db.Transactions, o => o.OrderId, t => t.OrderId);

            return db.Accounts.GroupJoin(subquery, a => a.AccountId, o => o.order.AccountId,
                (a, j) => ValueTuple.Create(a, j))
                .Where(x => x.Item1.AccountId < 100)
                .Flatten(x => x.Item2)
                .Join(db.Currencies, x => x.order.CurrencyId, c => c.CurrencyId)
                .Map(x => ValueTuple.Create(x.Item1.order, x.Item1.transaction, x.Item2));
        });



        (int accountId, int orderId)[] distinct = await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId)
                .DistinctBy(a => new { aId = a.Item2.AccountId, oId = a.Item1.AccountId })
                .Map(a => ValueTuple.Create(a.aId, a.oId)));

        (int, Order)[] distinct2 = await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId)
                .Map(a => a.Item2.AccountId)
                .Distinct()
                .Join(db.Orders, x => x, o => o.AccountId));

        (CurrencyType ct, int accountId, IEnumerable<(Order, Account)>)[] group = 
            await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId)
                .GroupBy(a => ValueTuple.Create(a.Item1.CurrencyId, a.Item2.AccountId))
                .Map(j => ValueTuple.Create(j.Key.Item1, j.Key.Item2, j.Items)));
        //.Join(db.Currencies, j => j.Item1, c => c.CurrencyId, (j, c) =>
        //ValueTuple.Create(j.Item1, j.Item2, j.Item3, c))

        (CurrencyType, int activeCount, decimal sum, Currency currency)[] group2 = await Query(db => db.Orders
                .Join(db.Accounts, o => o.AccountId, a => a.AccountId)
                .Map(x => ValueTuple.Create(x.Item1, x.Item2))
                .GroupBy(a => new { a.Item1.CurrencyId, a.Item2.AccountId }, g => new
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