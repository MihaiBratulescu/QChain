using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;
public class DefaultIfEmpty(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task NotEmpty_ReturnsRows()
    {
        var items = await Query(q => q.Accounts
            .Take(3)
            .DefaultIfEmpty());

        Assert.Equal(3, items.Length);
    }

    [Fact]
    public async Task EmptySource_ReturnsSingleNull()
    {
        var items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Null(items[0]);
    }

    [Fact]
    public async Task Mapping_Object_ReturnsSingleNull()
    {
       var items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.Email)
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Null(items[0]);
    }

    [Fact]
    public async Task Mapping_int_ReturnsSingleDefault()
    {
        int[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.AccountId)
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Equal(0, items[0]);
    }

    [Fact]
    public async Task Mapping_Nullable_ReturnsSingleNull()
    {
        int?[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.ClearanceLevel)
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Equal(0, items[0]);
    }

    [Fact]
    public async Task Mapping_Tuple_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
           Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => ValueTuple.Create(a.AccountId, a.IsActive))
            .DefaultIfEmpty())
        );
    }

    [Fact]
    public async Task GroupJoin()
    {
        (Account acc, Order? order)[] items = await Query(q => q.Accounts
            .GroupJoin(q.Orders.Where(o => o.OrderId > 100), a => a.AccountId, o => o.AccountId)
            .SelectMany(x => x.Item2.DefaultIfEmpty(), 
                        (x, order) => ValueTuple.Create(x.Item1, order)));

        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.NotNull(i.acc));
        Assert.All(items, i => Assert.Null(i.order));
    }
}
