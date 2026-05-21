using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class DefaultIfEmpty_WithValue(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task NotEmpty_ReturnsRows()
    {
        var items = await Query(q => q.Accounts
            .Take(3)
            .DefaultIfEmpty(new DatabaseModels.Account { AccountId = 0 }));

        Assert.Equal(3, items.Length);
    }

    [Fact]
    public async Task EmptySource_ReturnsSingleValue()
    {
        var items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .DefaultIfEmpty(new DatabaseModels.Account { AccountId = 0 }));

        Assert.Single(items);
        Assert.Null(items[0]);
    }

    [Fact]
    public async Task Mapping_Object_ReturnsSingleValue()
    {
        var items = await Query(q => q.Accounts
             .Where(a => a.AccountId > 100)
             .Select(a => a.Email)
             .DefaultIfEmpty("@email"));

        Assert.Single(items);
        Assert.Equal("@email", items[0]);
    }

    [Fact]
    public async Task Mapping_int_ReturnsSingleValue()
    {
        int[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.AccountId)
            .DefaultIfEmpty(7));

        Assert.Single(items);
        Assert.Equal(7, items[0]);
    }

    [Fact]
    public async Task Mapping_Nullable_ReturnsSingleNull()
    {
        int?[] items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .Select(a => a.ClearanceLevel)
            .DefaultIfEmpty(3));

        Assert.Single(items);
        Assert.Equal(3, items[0]);
    }

}
