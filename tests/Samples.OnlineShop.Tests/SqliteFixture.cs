using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Samples.OnlineShop.Database;
using Samples.OnlineShop.DatabaseModels;

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
            .UseAsyncSeeding((db, seed, ct) =>
            {
                if (seed)
                {
                    db.Set<Account>().AddRange(
                        new Account { IsActive = true, CreatedDate = DateTime.UtcNow },
                        new Account { IsActive = true, CreatedDate = DateTime.UtcNow },
                        new Account { IsActive = false, CreatedDate = DateTime.UtcNow });
                }

                return Task.CompletedTask;
            })
            .Options;

        db = new ApplicationDbContext(options);
        
        await db.Database.EnsureCreatedAsync();
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => connection.CloseAsync();
}