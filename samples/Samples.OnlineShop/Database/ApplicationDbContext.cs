using Microsoft.EntityFrameworkCore;
using QChain;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Database;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public IQueryable<Account> Accounts => Set<Account>().AsQuery();
    public IQueryable<AccountProfile> AccountProfiles => Set<AccountProfile>().AsQuery();
    public IQueryable<Order> Orders => Set<Order>().AsQuery();
    public IQueryable<Transaction> Transactions => Set<Transaction>().AsQuery();
    public IQueryable<Currency> Currencys => Set<Currency>().AsQuery();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>();
        modelBuilder.Entity<AccountProfile>();
        modelBuilder.Entity<Order>();
        modelBuilder.Entity<Transaction>();
        modelBuilder.Entity<Currency>();
    }
}
