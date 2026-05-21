using QChain;
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
    public async Task EmptySource_ReturnsProvidedValue()
    {
        DatabaseModels.Account value = new() { AccountId = 0 };

        var items = await Query(q => q.Accounts
            .Where(a => a.AccountId > 100)
            .DefaultIfEmpty(value));

        Assert.Single(items);
        Assert.Same(value, items[0]);
    }

    [Fact]
    public async Task Mapping_Object_ReturnsProvidedValue()
    {
        var items = await Query(q => q.Accounts
             .Where(a => a.AccountId > 100)
             .Select(a => a.Email!)
             .DefaultIfEmpty("@email"));

        Assert.Single(items);
        Assert.Equal("@email", items[0]);
    }
}
