using Microsoft.EntityFrameworkCore;
using QChain.Tests.TestBench;
using Xunit.Abstractions;

namespace QChain.Tests.Queries;

public class Filtering(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task Select()
    {
        var query = _fixture.db.Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.Email));

        var result = await query.ToArrayAsync();
    }

    [Fact]
    public async Task IsApplied()
    {
        var accounts = await Query(q => q.Accounts.Where(a => a.IsActive));

        Assert.NotEmpty(accounts);
        Assert.All(accounts, a => Assert.True(a.IsActive));
    }

    [Fact]
    public async Task NotMatched_ReturnsEmpty()
    {
        var accounts = await Query(q => q.Accounts
            .Where(a => a.CreatedDate < DateTime.UtcNow.AddYears(-10)));

        Assert.Empty(accounts);
    }

    [Fact]
    public async Task AfterMap_UsesProjectedShape()
    {
        var rows = await Query(
            q => q.Accounts.Select(a => new
            {
                Id = a.AccountId,
                Active = a.IsActive
            })
            .Where(x => x.Active));

        Assert.NotEmpty(rows);
        Assert.All(rows, a => Assert.True(a.Active));
    }

    [Fact]
    public async Task TupleProjection_NullFilters()
    {
        var rows = await Query(q => q.Accounts
            .Select(a => ValueTuple.Create(a.AccountId, a.Email, a.ClearanceLevel))
            .Where(x => x.Item2 == null && x.Item3 == null));

        Assert.Single(rows);
        Assert.Equal(6, rows[0].Item1);
        Assert.Null(rows[0].Item2);
        Assert.Null(rows[0].Item3);
    }

    [Fact]
    public async Task AnonymousProjection_NullCoalesce()
    {
        var rows = await Query(q => q.Accounts
            .Where(a => a.Email == null)
            .Select(a => new
            {
                a.AccountId,
                Email = a.Email ?? "missing"
            }));

        Assert.Single(rows);
        Assert.Equal("missing", rows[0].Email);
    }
}
