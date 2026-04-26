namespace Samples.OnlineShop.Tests;

public class Map(SqliteFixture fixture) : QChainIntegrationTestBench(fixture)
{
    [Fact]
    public async Task SingleColumn()
    {
        var accounts = await Query(q => q.Accounts.Map(a => a.AccountId));
        Assert.NotEmpty(accounts);
    }

    [Fact]
    public async Task SingleEntity()
    {
        var accounts = await Query(q => q.Accounts.Map(a => new { a.AccountId, a.Name }));
        Assert.NotEmpty(accounts);
    }

    [Fact]
    public async Task Join()
    {
        var results = await Query(q =>
            q.Accounts
             .Join(q.Orders, a => a.AccountId, o => o.AccountId)
             .Map(j => new { j.Item1.AccountId, orderAccountId = j.Item2.AccountId, j.Item2.OrderId }));

        Assert.NotEmpty(results);
        Assert.All(results, q => Assert.Equal(q.AccountId, q.orderAccountId));
    }

    [Fact]
    public async Task GroupJoin()
    {
        var results = await Query(q =>
            q.Accounts
             .GroupJoin(q.Orders, a => a.AccountId, o => o.AccountId)
             .Map(j => new 
             { 
                 j.Item1.AccountId, 
                 orderIds = j.Item2.Select(o => ValueTuple.Create(o.OrderId, o.AccountId)) 
             }));

        Assert.NotEmpty(results);
        foreach (var result in results)
        {
            Assert.All(result.orderIds, o => Assert.Equal(result.AccountId, o.Item2));
        }
    }
}
