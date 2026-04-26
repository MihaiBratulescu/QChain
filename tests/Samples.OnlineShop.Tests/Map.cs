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
        var accounts = await Query(q => q.Accounts.Map(a => ValueTuple.Create(a.AccountId, a.Name)));
        Assert.NotEmpty(accounts);
    }

    [Fact]
    public async Task AfterJoin()
    {
        (int accountId, int orderAccountId, int orderId)[] results = await Query(q =>
            q.Accounts
             .Join(q.Orders, a => a.AccountId, o => o.AccountId)
             .Map(j => ValueTuple.Create(j.Item1.AccountId, j.Item2.AccountId, j.Item2.OrderId)));

        Assert.NotEmpty(results);
        Assert.All(results, q => Assert.Equal(q.accountId, q.orderAccountId));
    }

    [Fact]
    public async Task BeforeJoin()
    {
        var results = await Query(q =>
            q.Accounts
             .Map(a => ValueTuple.Create(a.AccountId, a.CreatedDate))
             .Join(q.Orders, a => a.Item1, o => o.AccountId)
             .Map(j => new { j.Item1.Item1, orderAccountId = j.Item2.AccountId, j.Item2.OrderId }));

        Assert.NotEmpty(results);
        Assert.All(results, q => Assert.Equal(q.Item1, q.orderAccountId));
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
