using QChain;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests;

public class Executor(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    protected IQueryExecutor<Account> Entities => _fixture.db.Query(db => db.Accounts);

    [Fact]
    public async Task ToArray()
    {
        var array = await Entities.ToArrayAsync(default);        

        Assert.NotEmpty(array);
    }

    [Fact]
    public async Task Count()
    {
        var count = await Entities.CountAsync(default);
        var count2 = await Entities.CountAsync(a => a.AccountId == 1, default);

        Assert.True(count > 0);
        Assert.Equal(1, count2);
    }

    [Fact]
    public async Task FirstOrDefault()
    {
        var item = await Entities.FirstOrDefault(default);
        var item2 = await Entities.FirstOrDefault(a => a.AccountId == 1, default);

        Assert.NotNull(item);
        Assert.NotNull(item2);
    }

    [Fact]
    public async Task SingleOrDefault()
    {
        var item = await _fixture.db.Query(db => db.Accounts.Where(a => a.AccountId == 1))
            .SingleOrDefault(default);

        var item2 = await _fixture.db.Query(db => db.Accounts)
            .SingleOrDefault(a => a.AccountId == 1, default);

        Assert.NotNull(item);
    }

    [Fact]
    public async Task Any()
    {
        var check = await Entities.AnyAsync(default);
        var check2 = await Entities.AnyAsync(a => a.AccountId == 1, default);

        Assert.True(check);
        Assert.True(check2);
    }
}
