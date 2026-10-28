using LinqKit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NeinLinq;
using Samples.OnlineShop.Database;
using Samples.OnlineShop.DatabaseModels;
using System.Linq.Expressions;

namespace QChain.Tests.Integrations;

public class ExpressionLibraries
{
    public enum Expansion
    {
        NeinLinqBeforeQChain,
        NeinLinqAfterQChain,
        NeinLinqEfCore,
        LinqKitBeforeQChain,
        LinqKitAfterQChain,
        LinqKitEfCore
    }

    [Theory]
    [InlineData(Expansion.NeinLinqBeforeQChain)]
    [InlineData(Expansion.NeinLinqEfCore)]
    [InlineData(Expansion.LinqKitBeforeQChain)]
    [InlineData(Expansion.LinqKitAfterQChain)]
    [InlineData(Expansion.LinqKitEfCore)]
    public async Task BusinessFunctions_Compose_Across_Chained_Joins(Expansion expansion)
    {
        await using var database = await IntegrationDatabase.CreateAsync(expansion);
        var minimum = 100m;
        var payments = CustomerPayments(database)
            .Where(x => BusinessFunctions.IsActiveCustomer(x.account) && BusinessFunctions.HasMinimumTotal(x.order, minimum))
            .Where(x => BusinessFunctions.IsSettled(x.transaction))
            .OrderBy(x => x.order.OrderId)
            .Select(x => new PaymentRow(x.account, x.order, x.transaction));

        var rows = await payments.ToArrayAsync();

        Assert.Equal([1, 4], rows.Select(x => x.Order.OrderId));
        Assert.Equal([1, 3], rows.Select(x => x.Account.AccountId));
        Assert.Equal([100m, 250m], rows.Select(x => x.Transaction.Amount));
    }

    [Theory]
    [InlineData(Expansion.NeinLinqBeforeQChain)]
    [InlineData(Expansion.NeinLinqEfCore)]
    [InlineData(Expansion.LinqKitBeforeQChain)]
    [InlineData(Expansion.LinqKitAfterQChain)]
    [InlineData(Expansion.LinqKitEfCore)]
    public async Task Async_Terminal_Predicates_Expand_After_Tuple_Composition(Expansion expansion)
    {
        await using var database = await IntegrationDatabase.CreateAsync(expansion);
        var payments = CustomerPayments(database)
            .Where(x => BusinessFunctions.IsActiveCustomer(x.account) && BusinessFunctions.IsSettled(x.transaction));

        Assert.Equal(2, await payments.CountAsync(x => BusinessFunctions.HasMinimumTotal(x.order, 100m)));
        Assert.True(await payments.AnyAsync(x => BusinessFunctions.HasMinimumTotal(x.order, 250m)));
        Assert.False(await payments.AnyAsync(x => BusinessFunctions.HasMinimumTotal(x.order, 500m)));
    }

    [Theory]
    [InlineData(Expansion.NeinLinqBeforeQChain)]
    [InlineData(Expansion.NeinLinqEfCore)]
    [InlineData(Expansion.LinqKitBeforeQChain)]
    [InlineData(Expansion.LinqKitAfterQChain)]
    [InlineData(Expansion.LinqKitEfCore)]
    public async Task BusinessFunctions_Expand_Inside_Tuple_Projections(Expansion expansion)
    {
        await using var database = await IntegrationDatabase.CreateAsync(expansion);
        var rows = await CustomerPayments(database)
            .Where(x => BusinessFunctions.IsActiveCustomer(x.account) && BusinessFunctions.IsSettled(x.transaction))
            .Where(x => BusinessFunctions.HasMinimumTotal(x.order, 100m))
            .OrderBy(x => x.order.OrderId)
            .Select(x => ValueTuple.Create(BusinessFunctions.CustomerLabel(x.account), x.order.OrderId))
            .ToArrayAsync();

        Assert.Equal([("ONE", 1), ("THREE", 4)], rows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NeinLinq_Query_Wrapping_Must_Precede_AsQuery(bool scalarTerminal)
    {
        await using var database = await IntegrationDatabase.CreateAsync(Expansion.NeinLinqAfterQChain);
        var payments = CustomerPayments(database).Where(x => BusinessFunctions.IsActiveCustomer(x.account));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (scalarTerminal)
                _ = await payments.CountAsync();
            else
                _ = await payments.ToArrayAsync();
        });

        Assert.Contains("could not be translated", error.Message);
    }

    [Theory]
    [InlineData(Expansion.LinqKitBeforeQChain)]
    [InlineData(Expansion.LinqKitAfterQChain)]
    [InlineData(Expansion.LinqKitEfCore)]
    public async Task LinqKit_Expands_Invoked_Composed_Predicates_On_Joined_Entities(Expansion expansion)
    {
        await using var database = await IntegrationDatabase.CreateAsync(expansion);
        var minimum = 100m;
        var predicate = PredicateBuilder.And<Order>(
            order => order.Total >= minimum,
            order => order.CurrencyId == CurrencyType.EUR);

        var orderIds = await CustomerPayments(database)
            .Where(x => predicate.Invoke(x.order) && x.account.IsActive)
            .OrderBy(x => x.order.OrderId)
            .Select(x => x.order.OrderId)
            .ToArrayAsync();

        Assert.Equal([1, 4], orderIds);
    }

    private static IQueryable<(Account account, Order order, Transaction transaction)> CustomerPayments(IntegrationDatabase database)
    {
        IQueryable<(Account account, Order order)> customerOrders = database.Root<Account>()
            .Join(database.Root<Order>(), account => account.AccountId, order => order.AccountId,
                (account, order) => ValueTuple.Create(account, order));

        return customerOrders.Join(database.Root<Transaction>(), x => x.order.OrderId, transaction => transaction.OrderId,
            (x, transaction) => ValueTuple.Create(x.account, x.order, transaction));
    }

    public sealed record PaymentRow(Account Account, Order Order, Transaction Transaction);

    // These methods must be expanded into SQL; executing any of them in .NET fails the test.
    public static class BusinessFunctions
    {
        [InjectLambda(nameof(IsActiveCustomerExpression))]
        [Expandable(nameof(IsActiveCustomerExpression))]
        public static bool IsActiveCustomer(Account account) => throw new InvalidOperationException("Predicate was not expanded.");

        public static Expression<Func<Account, bool>> IsActiveCustomerExpression() => account => account.IsActive;

        [InjectLambda(nameof(HasMinimumTotalExpression))]
        [Expandable(nameof(HasMinimumTotalExpression))]
        public static bool HasMinimumTotal(Order order, decimal minimum) => throw new InvalidOperationException("Predicate was not expanded.");

        public static Expression<Func<Order, decimal, bool>> HasMinimumTotalExpression() => (order, minimum) => order.Total >= minimum;

        [InjectLambda(nameof(IsSettledExpression))]
        [Expandable(nameof(IsSettledExpression))]
        public static bool IsSettled(Transaction transaction) => throw new InvalidOperationException("Predicate was not expanded.");

        public static Expression<Func<Transaction, bool>> IsSettledExpression() => transaction => transaction.Status == TransactionStatus.Settled;

        [InjectLambda(nameof(CustomerLabelExpression))]
        [Expandable(nameof(CustomerLabelExpression))]
        public static string CustomerLabel(Account account) => throw new InvalidOperationException("Selector was not expanded.");

        public static Expression<Func<Account, string>> CustomerLabelExpression() => account => (account.Email ?? "unknown").ToUpper();
    }

    private sealed class IntegrationDatabase(SqliteConnection connection, ApplicationDbContext context, Expansion expansion) : IAsyncDisposable
    {
        public IQueryable<T> Root<T>() where T : class => expansion switch
        {
            Expansion.NeinLinqBeforeQChain => context.Set<T>().ToEntityInjectable().AsQuery(),
            Expansion.NeinLinqAfterQChain => context.Set<T>().AsQuery().ToEntityInjectable(),
            Expansion.LinqKitBeforeQChain => context.Set<T>().AsExpandable().AsQuery(),
            Expansion.LinqKitAfterQChain => context.Set<T>().AsQuery().AsExpandable(),
            _ => context.Set<T>().AsQuery()
        };

        public static async Task<IntegrationDatabase> CreateAsync(Expansion expansion)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection);

            if (expansion == Expansion.NeinLinqEfCore)
                options.WithLambdaInjection();
            if (expansion == Expansion.LinqKitEfCore)
                options.WithExpressionExpanding();

            var context = new ApplicationDbContext(options.Options);
            var database = new IntegrationDatabase(connection, context, expansion);
            try
            {
                await context.Database.EnsureCreatedAsync();
                context.Set<Account>().AddRange(
                    new Account { AccountId = 1, IsActive = true, Email = "one" },
                    new Account { AccountId = 2, IsActive = false, Email = "two" },
                    new Account { AccountId = 3, IsActive = true, Email = "three" });
                context.Set<Order>().AddRange(
                    new Order { OrderId = 1, AccountId = 1, Total = 100m, CurrencyId = CurrencyType.EUR },
                    new Order { OrderId = 2, AccountId = 1, Total = 200m, CurrencyId = CurrencyType.USD },
                    new Order { OrderId = 3, AccountId = 2, Total = 300m, CurrencyId = CurrencyType.EUR },
                    new Order { OrderId = 4, AccountId = 3, Total = 250m, CurrencyId = CurrencyType.EUR },
                    new Order { OrderId = 5, AccountId = 1, Total = 75m, CurrencyId = CurrencyType.EUR });
                context.Set<Transaction>().AddRange(
                    new Transaction { TransactionId = 1, OrderId = 1, Amount = 100m, Status = TransactionStatus.Settled },
                    new Transaction { TransactionId = 2, OrderId = 2, Amount = 200m, Status = TransactionStatus.Pending },
                    new Transaction { TransactionId = 3, OrderId = 3, Amount = 300m, Status = TransactionStatus.Settled },
                    new Transaction { TransactionId = 4, OrderId = 4, Amount = 250m, Status = TransactionStatus.Settled },
                    new Transaction { TransactionId = 5, OrderId = 5, Amount = 75m, Status = TransactionStatus.Settled });
                await context.SaveChangesAsync();
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
