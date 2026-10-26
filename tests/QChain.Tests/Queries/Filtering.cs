using Samples.OnlineShop.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using QChain.Tests.TestBench;
using Xunit.Abstractions;

namespace QChain.Tests.Queries;

public class Filtering(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task Select()
    {
        var query = _fixture.db.Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        var result = await query.ToArrayAsync();
    }

    [Fact]
    public async Task IsApplied()
    {
        var accounts = await Query(q => q.Accounts.Where(a => a.IsActive));

        Assert.NotEmpty(accounts);
        Assert.All(accounts, a => Assert.True(a.IsActive));
    }

    [Fact]
    public async Task NotMatched_ReturnsEmpty()
    {
        var accounts = await Query(q => q.Accounts
            .Where(a => a.CreatedDate < DateTime.UtcNow.AddYears(-10)));

        Assert.Empty(accounts);
    }

    [Fact]
    public async Task AfterMap_UsesProjectedShape()
    {
        var rows = await Query(
            q => q.Accounts.Select(a => new
            {
                Id = a.AccountId,
                Active = a.IsActive
            })
            .Where(x => x.Active));

        Assert.NotEmpty(rows);
        Assert.All(rows, a => Assert.True(a.Active));
    }

    [Fact]
    public async Task TupleProjection_NullFilters()
    {
        var rows = await Query(q => q.Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.Email, a.ClearanceLevel))
            .Where(x => x.Item2 == null && x.Item3 == null));

        Assert.Single(rows);
        Assert.Equal(6, rows[0].Item1);
        Assert.Null(rows[0].Item2);
        Assert.Null(rows[0].Item3);
    }

    [Fact]
    public async Task AnonymousProjection_NullCoalesce()
    {
        var rows = await Query(q => q.Accounts
            .Where(a => a.Email == null)
            .Select(a => new
            {
                a.AccountId,
                Email = a.Email ?? "missing"
            }));

        Assert.Single(rows);
        Assert.Equal("missing", rows[0].Email);
    }

    [Fact]
    public async Task Where_Predicate()
    {
        (Account a, Order p)[] predicate = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
            .Where(x => (!x.Item1.IsActive && x.Item1.AccountId % 2 == 0) || x.Item2.CurrencyId == CurrencyType.EUR));

        Assert.NotEmpty(predicate);
    }

    [Fact]
    public async Task Where_Predicate_ThenJoin()
    {
        var predicate = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow))
            .Where(x => (!x.Item1.IsActive && x.Item1.AccountId % 2 == 0) || x.Item2.CurrencyId == CurrencyType.EUR)
            .Join(q.Transactions, x => x.Item2.OrderId, t => t.TransactionId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow)));

        Assert.NotEmpty(predicate);
    }

    [Fact]
    public async Task Where_And()
    {
        Account[] accounts = await Query(q =>
            q.Accounts
                .Where(a => a.IsActive && a.AccountId % 2 == 0)
                .OrderBy(a => a.AccountId));

        Assert.Equal([2, 4, 6], accounts.Select(a => a.AccountId));
    }

    [Fact]
    public async Task Where_Or()
    {
        Account[] accounts = await Query(q =>
            q.Accounts
                .Where(a => !a.IsActive || a.Email == null)
                .OrderBy(a => a.AccountId));

        Assert.Equal([3, 5, 6], accounts.Select(a => a.AccountId));
    }
}
