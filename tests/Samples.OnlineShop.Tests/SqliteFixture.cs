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
            .UseAsyncSeeding(async (db, seed, _) =>
            {
                if (seed)
                {
                    DateTime now = DateTime.UtcNow;

                    await db.Set<Account>().AddRangeAsync(
                        new Account { AccountId = 1, Email = "Alpha", IsActive = true, CreatedDate = now.AddYears(-1) },
                        new Account { AccountId = 2, Email = "Beta", IsActive = true, CreatedDate = now.AddMonths(-6) },
                        new Account { AccountId = 3, Email = "Gamma", IsActive = false, CreatedDate = now.AddMonths(-2) },
                        new Account { AccountId = 4, Email = "Delta", IsActive = true, CreatedDate = now.AddDays(-10) },
                        new Account { AccountId = 5, Email = "Epsilon", IsActive = false, CreatedDate = now.AddDays(-1) },
                        new Account { AccountId = 6, Email = null!, IsActive = true, CreatedDate = now },
                        new Account { AccountId = 7, Email = "Epsilon", IsActive = true, CreatedDate = now });

                    await db.Set<AccountProfile>().AddRangeAsync(
                        new AccountProfile { AccountId = 1, AccountProfileId = 1, },
                        new AccountProfile { AccountId = 2, AccountProfileId = 2, },
                        new AccountProfile { AccountId = 3, AccountProfileId = 3, },
                        new AccountProfile { AccountId = 4, AccountProfileId = 4, },
                        new AccountProfile { AccountId = 5, AccountProfileId = 5, },
                        new AccountProfile { AccountId = 6, AccountProfileId = 6, },
                        new AccountProfile { AccountId = 7, AccountProfileId = 7, });

                    await db.Set<Order>().AddRangeAsync(
                        new Order { OrderId = 1, AccountId = 1, Total = 100, CurrencyId = CurrencyType.EUR, CreatedDate = now.AddMonths(-2) },
                        new Order { OrderId = 2, AccountId = 1, Total = 200, CurrencyId = CurrencyType.USD, CreatedDate = now.AddDays(-5) },
                        new Order { OrderId = 3, AccountId = 2, Total = 50, CurrencyId = CurrencyType.EUR, CreatedDate = now.AddDays(-2) },
                        new Order { OrderId = 4, AccountId = 2, Total = 75, CurrencyId = CurrencyType.ETH, CreatedDate = now.AddMonths(-1) },
                        new Order { OrderId = 5, AccountId = 3, Total = 300, CurrencyId = CurrencyType.BTC, CreatedDate = now.AddMonths(-3) },
                        // account with no orders → AccountId = 4
                        new Order { OrderId = 6, AccountId = 5, Total = 10, CurrencyId = CurrencyType.USD, CreatedDate = now.AddHours(-3) },
                        new Order { OrderId = 7, AccountId = 1, Total = 100, CurrencyId = CurrencyType.BTC, CreatedDate = now.AddDays(-1) }
                    );

                    await db.Set<Transaction>().AddRangeAsync(
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

                    await db.Set<Currency>().AddRangeAsync(
                        Enum.GetValues<CurrencyType>().Select(t => new Currency { CurrencyId = t, Symbol = t.ToString()}));
                }
            });

        db = new ApplicationDbContext(builder.Options);

        await db.Database.EnsureCreatedAsync();
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => connection.CloseAsync();
}