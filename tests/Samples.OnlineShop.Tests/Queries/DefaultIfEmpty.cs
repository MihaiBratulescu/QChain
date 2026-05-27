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
    public async Task Mapping_int_ReturnsProviderDefault()
    {
        int[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.AccountId)
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Equal(0, items[0]);
    }

    [Fact]
    public async Task Mapping_Nullable_ReturnsProviderDefault()
    {
        int?[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.ClearanceLevel)
            .DefaultIfEmpty());

        Assert.Single(items);
        // SQLite/EF compensates nullable scalar defaults as 0 here.
        Assert.Equal(0, items[0]);
    }

    [Fact]
    public async Task Mapping_Tuple_ReturnsComponentDefaults()
    {
        (int id, bool active)[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => ValueTuple.Create(a.AccountId, a.IsActive))
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Equal((0, false), items[0]);
    }

    [Fact]
    public async Task Mapping_MixedTuple_ReturnsComponentDefaults()
    {
        (int? clearance, string? email, int id)[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => ValueTuple.Create(a.ClearanceLevel, a.Email, a.AccountId))
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Equal((null, null, 0), items[0]);
    }

    [Fact]
    public async Task Mapping_NullableTuple_ReturnsNull()
    {
        (int id, bool active)?[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => (ValueTuple<int, bool>?)ValueTuple.Create(a.AccountId, a.IsActive))
            .DefaultIfEmpty());

        Assert.Single(items);
        Assert.Null(items[0]);
    }

    [Fact]
    public async Task Mapping_Tuple_ComposesAfterDefaultIfEmpty()
    {
        (int id, string email)[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => ValueTuple.Create(a.AccountId, a.Email))
            .DefaultIfEmpty()
            .Where(x => x.Item1 == 0)
            .Select(x => ValueTuple.Create(x.Item1, x.Item2 ?? "missing")));

        Assert.Single(items);
        Assert.Equal((0, "missing"), items[0]);
    }

    [Fact]
    public async Task Entity_ComposesAfterDefaultIfEmpty()
    {
        int[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .DefaultIfEmpty()
            .Where(a => a == null)
            .Select(a => a == null ? 0 : a.AccountId));

        Assert.Equal([0], items);
    }

    [Fact]
    public async Task Mapping_Object_ComposesAfterDefaultIfEmpty()
    {
        string[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.Email)
            .DefaultIfEmpty()
            .Where(email => email == null)
            .Select(email => email ?? "missing"));

        Assert.Equal(["missing"], items);
    }

    [Fact]
    public async Task Mapping_Int_ComposesAfterDefaultIfEmpty()
    {
        int[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.AccountId)
            .DefaultIfEmpty()
            .Where(id => id == 0)
            .Select(id => id + 1));

        Assert.Equal([1], items);
    }

    [Fact]
    public async Task Mapping_NullableTuple_ComposesAfterDefaultIfEmpty()
    {
        int[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => (ValueTuple<int, bool>?)ValueTuple.Create(a.AccountId, a.IsActive))
            .DefaultIfEmpty()
            .Where(x => x == null)
            .Select(x => x == null ? 0 : x.Value.Item1));

        Assert.Equal([0], items);
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
