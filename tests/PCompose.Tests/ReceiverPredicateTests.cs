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
            .Where(anon => anon.acc.IsActive().And(anon.ord.IsExpensive()))
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
            .Where(row => row.a2.IsActive().And(row.a3.IsActive().Not()))
            .ToArray();

        Assert.Equal([(null, new Account(true), new Account(false))], result);
    }

    [Theory]
    [InlineData(100, new int[] { 200 })]
    [InlineData(null, new int[] { })]
    public void GreaterThan_CapturedArgument(int? otherTotal, int[] expected)
    {
        Order? otherOrder = otherTotal.HasValue ? new Order(otherTotal.Value) : null;
        var items = new[] { new Order(50), new Order(100), new Order(200) }.AsQueryable();

        var result = items
            .Where(order => order.IsGreaterThan(otherOrder))
            .ToArray();

        Assert.Equal(expected, result.Select(order => order.Total).ToArray());
    }

    [Fact]
    public void GreaterThan_QueryMemberArgument()
    {
        var items = new (Order? order, Order? otherOrder)[]
        {
            (new Order(200), new Order(100)),
            (new Order(100), new Order(200)),
            (new Order(100), new Order(100)),
            (new Order(200), null),
            (null, new Order(100)),
            (null, null)
        }.AsQueryable();

        var result = items
            .Where(row => row.order.IsGreaterThan(row.otherOrder))
            .ToArray();

        Assert.Equal([(new Order(200), new Order(100))], result);
    }

    [Fact]
    public void GreaterThan_RepeatingTupleMembers()
    {
        var items = new (Order? a1, Order? a2, Order? a3)[]
        {
            (null, new Order(200), new Order(100)),
            (null, new Order(100), new Order(200)),
            (null, new Order(100), new Order(100)),
            (null, new Order(200), null),
            (null, null, new Order(100))
        }.AsQueryable();

        var result = items
            .Where(row => row.a2.IsGreaterThan(row.a3))
            .ToArray();

        Assert.Equal([(null, new Order(200), new Order(100))], result);
    }

    [Fact]
    public void GreaterThan_AnonymousMembers()
    {
        var items = new[]
        {
            new { Id = 1, order = new Order(200), other = new Order(100) },
            new { Id = 2, order = new Order(100), other = new Order(200) },
            new { Id = 3, order = new Order(100), other = new Order(100) }
        }.AsQueryable();

        var result = items
            .Where(row => row.order.IsGreaterThan(row.other))
            .ToArray();

        Assert.Equal([1], result.Select(row => row.Id).ToArray());
    }

    [Fact]
    public void GreaterThan_ComposesWithAnotherCondition()
    {
        var items = new[]
        {
            (order: new Order(200), other: new Order(100)),
            (order: new Order(150), other: new Order(200)),
            (order: new Order(50), other: new Order(25))
        }.AsQueryable();

        var result = items
            .Where(row => row.order.IsExpensive().And(row.order.IsGreaterThan(row.other)))
            .ToArray();

        Assert.Equal([(new Order(200), new Order(100))], result);
    }

    [Fact]
    public void GreaterThan_NullTarget()
    {
        var otherOrder = new Order(100);
        var items = new Order?[] 
        { 
            null, new Order(50), new Order(100), new Order(200) 
        }.AsQueryable();

        var result = items
            .Where(order => order.IsGreaterThan(otherOrder))
            .ToArray();

        Assert.Equal(new Order(200), Assert.Single(result));
    }
}

internal sealed record Order(int Total);
internal sealed record Account(bool Active);

internal static class Predicates
{
    extension(Account account)
    {
        public Expression<Func<Account, bool>> IsActive() => a => a.Active;
    }

    extension(Order order)
    {
        public Expression<Func<Order, bool>> IsExpensive() => a => a.Total > 100;
    }

    extension(Order? order)
    {
        public Expression<Func<Order?, Order?, bool>> IsGreaterThan(Order? otherOrder) =>
            (value, other) => value != null && other != null && value.Total > other.Total;
    }
}
