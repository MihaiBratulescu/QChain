namespace Samples.OnlineShop.Tests;

public class QChainIntegrationTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;

    public QChainIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EntityQuery_NotEmpty()
    {
        var accounts = await _fixture.db
            .Query(q => q.Accounts)
            .ToArrayAsync(default);

        Assert.NotEmpty(accounts);
    }

    public class Filtering(SqliteFixture fixture) : QChainIntegrationTests(fixture)
    {
        [Fact]
        public async Task IsApplied()
        {
            var accounts = await _fixture.db
                .Query(q => q.Accounts.Active())
                .ToArrayAsync(default);

            Assert.NotEmpty(accounts);
            Assert.All(accounts, a => Assert.True(a.IsActive));
        }

        [Fact]
        public async Task NotMatched_ReturnsEmpty()
        {
            var accounts = await _fixture.db
                .Query(q => q.Accounts
                    .Where(a => a.CreatedDate < DateTime.UtcNow.AddYears(-10)))
                .ToArrayAsync(default);

            Assert.Empty(accounts);
        }

        [Fact]
        public async Task AfterMap_UsesProjectedShape()
        {
            var rows = await _fixture.db
                .Query(q => q.Accounts
                    .Map(a => new
                    {
                        Id = a.AccountId,
                        Active = a.IsActive
                    })
                    .Where(x => x.Active))
                .ToArrayAsync(default);

            Assert.NotEmpty(rows);
            Assert.All(rows, a => Assert.True(a.Active));
        }
    }

    
}
