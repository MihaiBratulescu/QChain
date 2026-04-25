using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Samples.OnlineShop.Database;

namespace Samples.OnlineShop.Tests;

public sealed class SqliteFixture : IAsyncLifetime
{
    public ApplicationDbContext db = null!;
    private SqliteConnection connection = new("Data Source=:memory:");

    public async Task InitializeAsync()
    {
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .UseSeeding((db, seed) =>
            {
                if (seed)
                {

                }
            })
            .Options;

        db = new ApplicationDbContext(options);
        
        await db.Database.EnsureCreatedAsync();
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => connection.CloseAsync();
}