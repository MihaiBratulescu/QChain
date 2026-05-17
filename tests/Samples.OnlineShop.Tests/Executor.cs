using QChain;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests;

public class Executor(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    protected IQuery<Account> Accounts => _fixture.db.Accounts;

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
    public async Task FirstOrDefault()
    {
        var item = await Accounts.FirstOrDefaultAsync(default);
        var item2 = await Accounts.FirstOrDefaultAsync(a => a.AccountId == 1, default);

        Assert.NotNull(item);
        Assert.NotNull(item2);
    }

    [Fact]
    public async Task SingleOrDefault()
    {
        var item = await _fixture.db.Query(db => db.Accounts.Where(a => a.AccountId == 1))
            .SingleOrDefaultAsync(default);

        var item2 = await _fixture.db.Query(db => db.Accounts)
            .SingleOrDefaultAsync(a => a.AccountId == 1, default);

        Assert.NotNull(item);
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
        
        var items = await Accounts.AsNoTracking().AsTracking().ToArrayAsync(default);

        Assert.NotEmpty(items);
        Assert.Equal(items.Length, _fixture.db.ChangeTracker.Entries().Count());
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
