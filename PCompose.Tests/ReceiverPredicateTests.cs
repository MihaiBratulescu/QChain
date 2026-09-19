using System.Linq.Expressions;

namespace PCompose.Tests;

public class PredicateTargetTests
{
    [Fact]
    public void AnonymousMembers()
    {
        var items = new[]
        {
            new { Id = 1, acc = new Account(true), ord = new Order(200) },
            new { Id = 2, acc = new Account(false), ord = new Order(200) },
            new { Id = 3, acc = new Account(true), ord = new Order(50) },
            new { Id = 4, acc = new Account(false), ord = new Order(50) }
        }.AsQueryable();

        var result = items
            .Where(anon => anon.acc.IsActive()
                .And(anon.ord.IsExpensive()))
            .ToArray();

        Assert.Equal([1], result.Select(row => row.Id).ToArray());
    }

    [Fact]
    public void TupleMembers()
    {
        var items = new[]
        {
            (account: new Account(true), order: new Order(200)),
            (account: new Account(false), order: new Order(200)),
            (account: new Account(true), order: new Order(50)),
            (account: new Account(false), order: new Order(50))
        }.AsQueryable();

        var result = items
            .Where(row => row.account.IsActive().And(row.order.IsExpensive()))
            .ToArray();

        Assert.Equal([(new Account(true), new Order(200))], result);
    }
    [Fact]
    public void RepeatingTupleMembers()
    {
        var items = new (Account? a1, Account a2, Account a3)[]
        {
            (null, new Account(true), new Account(true)),
            (null, new Account(false), new Account(true)),
            (null, new Account(true), new Account(false)),
            (null, new Account(false), new Account(false))
        }.AsQueryable();

        var result = items
            .Where(row => row.a2.IsActive().And(row.a3.IsActive()))
            .ToArray();

        Assert.Equal([(null, new Account(true), new Account(true))], result);
    }
}

internal sealed record Order(int Total);
internal sealed record Account(bool Active);

internal static class Predicates
{
    extension(Account account)
    {
        public Expression<Func<Account, bool>> IsActive() => value => value.Active;
    }

    extension(Order order)
    {
        public Expression<Func<Order, bool>> IsExpensive() => value => value.Total > 100;
    }
}
