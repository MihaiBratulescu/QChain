namespace Samples.OnlineShop.Tests;

public class UnitTest1 : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;

    public UnitTest1(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test1()
    {
        var accounts = await _fixture.db
            .Query(q => q.Accounts)
            .ToArrayAsync(default);

        Assert.NotEmpty(accounts);
    }
}
