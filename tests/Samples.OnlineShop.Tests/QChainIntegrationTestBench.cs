using QChain;
using Samples.OnlineShop.Database;
using Xunit.Abstractions;

namespace Samples.OnlineShop.Tests;

public class QChainIntegrationTestBench : IClassFixture<SqliteFixture>
{
    protected readonly SqliteFixture _fixture;

    public QChainIntegrationTestBench(SqliteFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.output = output;
    }

    protected Task<T[]> Query<T>(Func<IUnitOfWork, IQuery<T>> query) =>
        _fixture.db.Query(query).ToArrayAsync(default);
}
