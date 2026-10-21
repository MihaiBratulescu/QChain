using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests.Queries;

public class Distinct(SqliteFixture fixture, ITestOutputHelper output) : QChainIntegrationTestBench(fixture, output)
{
    [Fact]
    public async Task TupleProjection_ThenGroupBy()
    {
        var rows = await Query(q => q.Accounts
            .Join(q.Orders, a => a.AccountId, o => o.AccountId)
            .Select(x => ValueTuple.Create(x.Item1.AccountId, x.Item2.CurrencyId))
            .Distinct()
            .GroupBy(
                x => x.Item2,
                g => ValueTuple.Create(g.Key, g.Count()))
            .OrderBy(x => x.Item1));

        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.True(x.Item2 > 0));
    }
}
