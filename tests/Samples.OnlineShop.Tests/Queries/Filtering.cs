using PCompose;
using Samples.OnlineShop.DatabaseModels;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;
public class Filtering(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task Where_Predicate()
    {
        Expression<Func<Account, bool>> inactive = a => a.IsActive == false;
        Expression<Func<Account, bool>> even = a => a.AccountId % 2 == 0;
        Expression<Func<Order, bool>> euro = o => o.CurrencyId == CurrencyType.EUR;

        (Account a, Order p)[] predicate = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Where(x => inactive.And(even).Or(euro)));

        Assert.NotEmpty(predicate);
    }

    [Fact]
    public async Task Where_Predicate_ThenJoin()
    {
        Expression<Func<Account, bool>> inactive = a => a.IsActive == false;
        Expression<Func<Account, bool>> even = a => a.AccountId % 2 == 0;
        Expression<Func<Order, bool>> euro = o => o.CurrencyId == CurrencyType.EUR;

        var predicate = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Where(x => inactive.And(even).Or(euro))
            .Join(q.Transactions, x => x.Item2.OrderId, t => t.TransactionId));

        Assert.NotEmpty(predicate);
    }

    [Fact]
    public async Task Where_InvalidPredicate_Throws()
    {
        Expression<Func<Account, bool>> inactive = a => a.IsActive == false;
        Expression<Func<Transaction, bool>> invalid = t => t.Amount > 100;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Query(q => q.Accounts
                .Join(q.Orders, a => a.AccountId, o => o.AccountId)
                .Where(x => inactive.And(invalid)))
        );
    }

    [Fact]
    public async Task And_ComposesPredicates()
    {
        Expression<Func<Account, bool>> active = a => a.IsActive;
        Expression<Func<Account, bool>> evenId = a => a.AccountId % 2 == 0;

        Account[] accounts = await Query(q =>
            q.Accounts
                .Where(x => active.And(evenId))
                .OrderBy(a => a.AccountId));

        Assert.Equal([2, 4, 6], accounts.Select(a => a.AccountId));
    }

    [Fact]
    public async Task Or_ComposesPredicates()
    {
        Expression<Func<Account, bool>> inactive = a => !a.IsActive;
        Expression<Func<Account, bool>> nullEmail = a => a.Email == null;

        Account[] accounts = await Query(q =>
            q.Accounts
                .Where(x => inactive.Or(nullEmail))
                .OrderBy(a => a.AccountId));

        Assert.Equal([3, 5, 6], accounts.Select(a => a.AccountId));
    }
}
