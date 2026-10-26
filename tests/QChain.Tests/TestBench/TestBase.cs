using Microsoft.EntityFrameworkCore;
using Samples.OnlineShop.Database;
using Xunit.Abstractions;

namespace QChain.Tests.TestBench;

public class TestBase : IClassFixture<SqliteFixture>
{
    protected readonly SqliteFixture _fixture;

    public TestBase(SqliteFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.output = output;
    }

    public Task<T[]> Query<T>(Func<ApplicationDbContext, IQueryable<T>> query) =>
        query(_fixture.db).ToArrayAsync();
}
