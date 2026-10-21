using QChain.Tests.TestBench;
using Microsoft.EntityFrameworkCore;
using QChain;
using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace QChain.Tests;

public class Execution(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    protected IQueryable<Account> Accounts => _fixture.db.Accounts;

    [Fact]
    public async Task ToArray()
    {
        var array = await Accounts.ToArrayAsync(default);

        Assert.NotEmpty(array);
    }

    [Fact]
    public async Task Count()
    {
        var count = await Accounts.CountAsync(default);
        var count2 = await Accounts.CountAsync(a => a.AccountId == 1, default);

        Assert.True(count > 0);
        Assert.Equal(1, count2);
    }

    [Fact]
    public async Task Count_AfterProjection()
    {
        var count = await Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.IsActive))
            .CountAsync(x => x.Item1 > 0, default);

        Assert.Equal(await Accounts.CountAsync(default), count);
    }

    [Fact]
    public async Task CountAsync_Forwards_Cancellation_AfterProjection()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Accounts
            .Select(account => ValueTuple.Create(account.AccountId, account.IsActive))
            .CountAsync(account => account.Item1 > 0, cancellation.Token));
    }

    [Fact]
    public async Task LongCount_AfterProjection()
    {
        var count = await Accounts
            .Select(a => new { Id = a.AccountId, a.IsActive })
            .LongCountAsync(x => x.Id > 0, default);

        Assert.Equal(await Accounts.LongCountAsync(default), count);
    }

    [Fact]
    public async Task FirstOrDefault()
    {
        var item = await Accounts.FirstOrDefaultAsync(default);
        var item2 = await Accounts.FirstOrDefaultAsync(a => a.AccountId == 1, default);

        Assert.NotNull(item);
        Assert.NotNull(item2);
    }

    [Fact]
    public async Task FirstOrDefault_AfterProjection()
    {
        var item = await Accounts
            .OrderBy(a => a.AccountId)
            .Select(a => ValueTuple.Create(a.AccountId, a.Email))
            .FirstOrDefaultAsync(x => x.Item1 == 1, default);

        Assert.Equal(1, item.Item1);
    }

    [Fact]
    public async Task SingleOrDefault()
    {
        var item = await _fixture.db.Accounts.Where(a => a.AccountId == 1)
            .SingleOrDefaultAsync(default);

        var item2 = await _fixture.db.Accounts
            .SingleOrDefaultAsync(a => a.AccountId == 1, default);

        Assert.NotNull(item);
    }

    [Fact]
    public async Task SingleOrDefault_AfterProjection()
    {
        var item = await Accounts
            .Select(a => new { Id = a.AccountId, a.Email })
            .SingleOrDefaultAsync(a => a.Id == 1, default);

        Assert.NotNull(item);
        Assert.Equal(1, item.Id);
    }

    [Fact]
    public async Task Any()
    {
        var check = await Accounts.AnyAsync(default);
        var check2 = await Accounts.AnyAsync(a => a.AccountId == 1, default);

        Assert.True(check);
        Assert.True(check2);
    }

    [Fact]
    public async Task Any_AfterProjection()
    {
        var check = await Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.IsActive))
            .AnyAsync(x => x.Item1 == 1, default);

        Assert.True(check);
    }

    [Fact]
    public async Task TerminalPredicate_Async()
    {
        var accounts = Accounts.OrderBy(a => a.AccountId).Select(a => a);

        Assert.True(await accounts.AnyAsync(a => a.IsActive && a.AccountId % 2 == 0, default));
        Assert.Equal(3, await accounts.CountAsync(a => a.IsActive && a.AccountId % 2 == 0, default));
        Assert.Equal(3L, await accounts.LongCountAsync(a => a.IsActive && a.AccountId % 2 == 0, default));
        Assert.Equal(2, (await accounts.FirstAsync(a => a.IsActive && a.AccountId % 2 == 0, default)).AccountId);
        Assert.Equal(6, (await accounts.LastAsync(a => a.IsActive && a.AccountId % 2 == 0, default)).AccountId);
        Assert.Equal(2, (await accounts.SingleAsync(a => a.IsActive && a.AccountId == 2, default)).AccountId);
    }

    [Fact]
    public void TerminalPredicate_Sync_AfterJoin()
    {
        IQueryable<(Account account, Order order)> joined = _fixture.db.Accounts
            .Join(_fixture.db.Orders, a => a.AccountId, o => o.AccountId, (leftRow, rightRow) => ValueTuple.Create(leftRow, rightRow));

        Assert.True(joined.Any(x => x.account.IsActive && x.order.CurrencyId == CurrencyType.EUR));
        Assert.Equal(2, joined.Count(x => x.account.IsActive && x.order.CurrencyId == CurrencyType.EUR));
        Assert.NotNull(joined.FirstOrDefault(x => x.account.IsActive && x.order.CurrencyId == CurrencyType.EUR).account);
    }

    [Fact]
    public async Task Aggregates_AfterProjection()
    {
        var max = await Accounts.MaxAsync(a => a.AccountId, default);
        var min = await Accounts.MinAsync(a => a.AccountId, default);
        var sum = await Accounts.SumAsync(a => a.AccountId, default);
        var average = await Accounts.AverageAsync(a => (double)a.AccountId, default);

        var projected = Accounts.Select(a => new { Id = a.AccountId, a.IsActive });

        Assert.Equal(max, await projected.MaxAsync(a => a.Id, default));
        Assert.Equal(min, await projected.MinAsync(a => a.Id, default));
        Assert.Equal(sum, await projected.SumAsync(a => a.Id, default));
        Assert.Equal(average, await projected.AverageAsync(a => (double)a.Id, default));
    }

    [Fact]
    public async Task MinMax_EmptyNullableProjection_ReturnsNull()
    {
        var empty = Accounts.Where(a => a.AccountId > 100);

        Assert.Null(await empty.MinAsync(a => a.ClearanceLevel, default));
        Assert.Null(await empty.MaxAsync(a => a.ClearanceLevel, default));
    }

    [Fact]
    public async Task MinMax_EmptyNonNullableProjection_Throws()
    {
        var empty = Accounts.Where(a => a.AccountId > 100);

        await Assert.ThrowsAsync<InvalidOperationException>(() => empty.MinAsync(a => a.AccountId, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => empty.MaxAsync(a => a.AccountId, default));
    }

    [Fact]
    public async Task ElementAt_AfterProjection()
    {
        var item = await Accounts
            .OrderBy(a => a.AccountId)
            .Select(a => ValueTuple.Create(a.AccountId, a.Email))
            .ElementAtAsync(1, default);

        Assert.Equal(2, item.Item1);
    }

    [Fact]
    public async Task ToList_AfterProjection()
    {
        var items = await Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.Email))
            .ToListAsync(default);

        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.True(x.Item1 > 0));
    }

    [Fact]
    public async Task NoTracking()
    {
        _fixture.db.ChangeTracker.Clear();

        var items = await Accounts.AsNoTracking().ToArrayAsync(default);

        Assert.NotEmpty(items);
        Assert.False(_fixture.db.ChangeTracker.Entries().Any());
    }

    [Fact]
    public async Task TrackedEntities()
    {
        _fixture.db.ChangeTracker.Clear();
        _fixture.db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var items = await Accounts.AsNoTracking().AsTracking().ToArrayAsync(default);

        Assert.NotEmpty(items);
        Assert.Equal(items.Length, _fixture.db.ChangeTracker.Entries().Count());
    }

    [Fact]
    public async Task NoTracking_AfterJoin_Composes_With_TupleProjection()
    {
        _fixture.db.ChangeTracker.Clear();

        var items = await Accounts
            .Join(_fixture.db.Orders, account => account.AccountId, order => order.AccountId, (account, order) => account)
            .AsNoTracking()
            .Where(account => account.AccountId == 1)
            .Select(account => ValueTuple.Create(account.AccountId, account))
            .ToArrayAsync();

        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.Equal(1, item.Item1));
        Assert.Empty(_fixture.db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task NoTrackingWithIdentityResolution_AfterJoin()
    {
        _fixture.db.ChangeTracker.Clear();

        var items = await Accounts
            .Join(_fixture.db.Orders, account => account.AccountId, order => order.AccountId, (account, order) => account)
            .AsNoTrackingWithIdentityResolution()
            .Where(account => account.AccountId == 1)
            .ToArrayAsync();

        Assert.True(items.Length > 1);
        Assert.All(items, item => Assert.Same(items[0], item));
        Assert.Empty(_fixture.db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Include()
    {
        _fixture.db.ChangeTracker.Clear();

        var items = await Accounts.Include(a => a.Profile).ToArrayAsync(default);

        Assert.NotEmpty(items);
        Assert.All(items, a => Assert.NotNull(a.Profile));
    }
}
