using QChain;
using PCompose;
using Samples.OnlineShop.DatabaseModels;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests;

public class Executor(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    protected IQuery<Account> Accounts => _fixture.db.Accounts;

    [Fact]
    public async Task TerminalPredicate_Async()
    {
        Expression<Func<Account, bool>> active = a => a.IsActive;
        Expression<Func<Account, bool>> evenId = a => a.AccountId % 2 == 0;
        Expression<Func<Account, bool>> account2 = a => a.AccountId == 2;

        IQueryExecutor<Account> accounts = _fixture.db.Query(db => db.Accounts.OrderBy(a => a.AccountId).Select(a => a));

        Assert.True(await accounts.AnyAsync(x => active.And(evenId), default));
        Assert.Equal(3, await accounts.CountAsync(x => active.And(evenId), default));
        Assert.Equal(3L, await accounts.LongCountAsync(x => active.And(evenId), default));
        Assert.Equal(2, (await accounts.FirstAsync(x => active.And(evenId), default)).AccountId);
        Assert.Equal(6, (await accounts.LastAsync(x => active.And(evenId), default)).AccountId);
        Assert.Equal(2, (await accounts.SingleAsync(x => active.And(account2), default)).AccountId);
    }
}
