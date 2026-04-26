using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Samples.OnlineShop.Database;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Tests;

public sealed class SqliteFixture : IAsyncLifetime
{
    public ApplicationDbContext db = null!;
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public async Task InitializeAsync()
    {
        connection.Open();

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .UseAsyncSeeding((db, seed, ct) =>
            {
                if (seed)
                {
                    DateTime now = DateTime.UtcNow;

                    db.Set<Account>().AddRange(
                        new Account { AccountId = 1, Name = "Alpha", IsActive = true, CreatedDate = now.AddYears(-1) },
                        new Account { AccountId = 2, Name = "Beta", IsActive = true, CreatedDate = now.AddMonths(-6) },
                        new Account { AccountId = 3, Name = "Gamma", IsActive = false, CreatedDate = now.AddMonths(-2) },
                        new Account { AccountId = 4, Name = "Delta", IsActive = true, CreatedDate = now.AddDays(-10) },
                        new Account { AccountId = 5, Name = "Epsilon", IsActive = false, CreatedDate = now.AddDays(-1) },
                        new Account { AccountId = 6, Name = null!, IsActive = true, CreatedDate = now });

                    db.Set<Order>().AddRange(
                        new Order { OrderId = 1, AccountId = 1, Total = 100, CreatedDate = now.AddMonths(-2) },
                        new Order { OrderId = 2, AccountId = 1, Total = 200, CreatedDate = now.AddDays(-5) },
                        new Order { OrderId = 3, AccountId = 2, Total = 50, CreatedDate = now.AddDays(-2) },
                        new Order { OrderId = 4, AccountId = 2, Total = 75, CreatedDate = now.AddMonths(-1) },
                        new Order { OrderId = 5, AccountId = 3, Total = 300, CreatedDate = now.AddMonths(-3) },
                        // account with no orders → AccountId = 4
                        new Order { OrderId = 6, AccountId = 5, Total = 10, CreatedDate = now.AddHours(-3) },
                        new Order { OrderId = 7, AccountId = 1, Total = 100, CreatedDate = now.AddDays(-1) }
                    );

                    db.Set<Transaction>().AddRange(
                        new Transaction { TransactionId = 1, OrderId = 1, Status = TransactionStatus.Settled, Amount = 100, CreatedDate = now.AddDays(-9) },
                        new Transaction { TransactionId = 2, OrderId = 1, Status = TransactionStatus.Refunded, Amount = -20, CreatedDate = now.AddDays(-8) },

                        new Transaction { TransactionId = 3, OrderId = 2, Status = TransactionStatus.Pending, Amount = 200, CreatedDate = now.AddDays(-1) },

                        new Transaction { TransactionId = 4, OrderId = 3, Status = TransactionStatus.Settled, Amount = 50, CreatedDate = now.AddDays(-1) },
                        new Transaction { TransactionId = 5, OrderId = 3, Status = TransactionStatus.Settled, Amount = 50, CreatedDate = now.AddHours(-10) },

                        new Transaction { TransactionId = 6, OrderId = 4, Status = TransactionStatus.Failed, Amount = 75, CreatedDate = now.AddMonths(-2) },

                        // order with no transactions → OrderId = 6

                        // mixed statuses same order
                        new Transaction { TransactionId = 7, OrderId = 7, Status = TransactionStatus.Settled, Amount = 100, CreatedDate = now.AddHours(-3) },
                        new Transaction { TransactionId = 8, OrderId = 7, Status = TransactionStatus.Pending, Amount = 100, CreatedDate = now.AddHours(-2) }
                    );
                }

                return Task.CompletedTask;
            });

        db = new ApplicationDbContext(builder.Options);
        
        await db.Database.EnsureCreatedAsync();
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => connection.CloseAsync();
}