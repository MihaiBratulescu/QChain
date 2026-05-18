using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests.Queries;

public class GroupBy(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task OnTable()
    {
        (string? name, IEnumerable<Account> accounts)[] result = await Query(q =>
            q.Accounts.GroupBy(a => a.Email));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q.accounts, a => Assert.Equal(q.name, a.Email)));
    }

    [Fact]
    public async Task OnTuple()
    {
        (string? name, IEnumerable<(string? name, bool isActive)> accounts)[] result = await Query(q =>
            q.Accounts
             .Select(a => ValueTuple.Create(a.Email, a.IsActive))
             .GroupBy(a => a.Item1));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q.accounts, a => Assert.Equal(q.name, a.name)));
    }

    [Fact]
    public async Task TupleKey()
    {
        ((string? name, bool isActive) key, IEnumerable<Account> accounts)[] result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Email, a.IsActive)));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.All(q.accounts, a => Assert.Equal(q.key.name, a.Email)));
    }

    [Fact]
    public async Task TupleKey_Projected()
    {
        ((string?, bool), int total)[] result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Email, a.IsActive),
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
            q.Accounts.GroupBy(a => a.Email, a => ValueTuple.Create(a.Key, a.Count())));

        Assert.NotEmpty(result);
        Assert.All(result, q => Assert.True(q.count > 0) );
    }

    [Fact]
    public async Task WithJoin()
    {
        var result = await Query(q =>
            q.Accounts.GroupBy(a => ValueTuple.Create(a.Email, a.IsActive),
                               g => new { g.Key, total = g.Count(), first = g.Min(a => a.AccountId) })
                      .GroupJoin(q.Orders, g => g.first, o => o.AccountId));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Join_GroupOnTupleKey()
    {
        ((CurrencyType currencyId, int accountId) Key, IEnumerable<(Order, Account)> Items)[] result =
            await Query(q => q.Orders
                .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
                .GroupBy(x => ValueTuple.Create(x.Item1.CurrencyId, x.Item2.AccountId)));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Join_GroupOnTupleKey_ValueTuple()
    {
        ((CurrencyType currencyId, int accountId) Key, IEnumerable<(Order, Account)> Items)[] result =
            await Query(q => q.Orders
                .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
                .GroupBy(x => new ValueTuple<CurrencyType, int>(x.Item1.CurrencyId, x.Item2.AccountId)));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Aggregate_ThenJoin()
    {
        (CurrencyType currencyId, int activeCount, Currency currency)[] result =
            await Query(q => q.Orders
                .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
                .GroupBy(
                    x => new { x.Item1.CurrencyId, x.Item2.AccountId },
                    g => new
                    {
                        g.Key,
                        ActiveCount = g.Count(x => x.Item2.IsActive)
                    })
                .Join(q.Currencies,
                    g => g.Key.CurrencyId,
                    c => c.CurrencyId,
                    (g, c) => ValueTuple.Create(g.Key.CurrencyId, g.ActiveCount, c)));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task TupleMap_AfterGroupJoin()
    {
        (CurrencyType currencyId, int activeCount, Currency currency)[] result =
            await Query(q => q.Orders
                .Join(q.Accounts, o => o.AccountId, a => a.AccountId)
                .GroupBy(
                    x => new { x.Item1.CurrencyId },
                    g => new
                    {
                        g.Key.CurrencyId,
                        ActiveCount = g.Count(x => x.Item2.IsActive)
                    })
                .Join(q.Currencies,
                    g => g.CurrencyId,
                    c => c.CurrencyId,
                    (g, c) => new { g.CurrencyId, g.ActiveCount, Currency = c })
                .Select(x => ValueTuple.Create(x.CurrencyId, x.ActiveCount, x.Currency)));

        Assert.NotEmpty(result);
    }
}
