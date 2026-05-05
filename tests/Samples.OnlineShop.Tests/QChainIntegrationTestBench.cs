using QChain;
using Samples.OnlineShop.Database;

namespace Samples.OnlineShop.Tests;

public class QChainIntegrationTestBench(SqliteFixture fixture) : IClassFixture<SqliteFixture>
{
    protected readonly SqliteFixture _fixture = fixture;

    protected Task<T[]> Query<T>(Func<IUnitOfWork, IQuery<T>> query) =>
        _fixture.db.Query(query).ToArrayAsync(default);
}
