using System.Linq.Expressions;

namespace PCompose.Tests;

public class BooleanAlgebraTests
{
    // The three bits cover every combination of a, b, and c.
    private static readonly IQueryable<int> items = Enumerable.Range(0, 8).AsQueryable();
    private static readonly Expression<Func<int, bool>> a = x => (x & 1) != 0;
    private static readonly Expression<Func<int, bool>> b = x => (x & 2) != 0;
    private static readonly Expression<Func<int, bool>> c = x => (x & 4) != 0;
    private static readonly Expression<Func<int, bool>> alwaysTrue = x => true;
    private static readonly Expression<Func<int, bool>> alwaysFalse = x => false;

    [Fact]
    public void DeMorgan_NotAnd_EqualsOrOfNots()
    {
        AssertEquivalent(a.And(b).Not(), a.Not().Or(b.Not()), [0, 1, 2, 4, 5, 6]);
    }

    [Fact]
    public void DeMorgan_NotOr_EqualsAndOfNots()
    {
        AssertEquivalent(a.Or(b).Not(), a.Not().And(b.Not()), [0, 4]);
    }

    [Fact]
    public void DoubleNegation()
    {
        AssertEquivalent(a.Not().Not(), a, [1, 3, 5, 7]);
    }

    [Fact]
    public void And_Commutativity()
    {
        AssertEquivalent(a.And(b), b.And(a), [3, 7]);
    }

    [Fact]
    public void Or_Commutativity()
    {
        AssertEquivalent(a.Or(b), b.Or(a), [1, 2, 3, 5, 6, 7]);
    }

    [Fact]
    public void And_Associativity()
    {
        AssertEquivalent(a.And(b).And(c), a.And(b.And(c)), [7]);
    }

    [Fact]
    public void Or_Associativity()
    {
        AssertEquivalent(a.Or(b).Or(c), a.Or(b.Or(c)), [1, 2, 3, 4, 5, 6, 7]);
    }

    [Fact]
    public void And_DistributesOverOr()
    {
        AssertEquivalent(a.And(b.Or(c)), a.And(b).Or(a.And(c)), [3, 5, 7]);
    }

    [Fact]
    public void Or_DistributesOverAnd()
    {
        AssertEquivalent(a.Or(b.And(c)), a.Or(b).And(a.Or(c)), [1, 3, 5, 6, 7]);
    }

    [Fact]
    public void And_Idempotence()
    {
        AssertEquivalent(a.And(a), a, [1, 3, 5, 7]);
    }

    [Fact]
    public void Or_Idempotence()
    {
        AssertEquivalent(a.Or(a), a, [1, 3, 5, 7]);
    }

    [Fact]
    public void And_Identity()
    {
        AssertEquivalent(a.And(alwaysTrue), a, [1, 3, 5, 7]);
    }

    [Fact]
    public void Or_Identity()
    {
        AssertEquivalent(a.Or(alwaysFalse), a, [1, 3, 5, 7]);
    }

    [Fact]
    public void And_Domination()
    {
        AssertEquivalent(a.And(alwaysFalse), alwaysFalse, []);
    }

    [Fact]
    public void Or_Domination()
    {
        AssertEquivalent(a.Or(alwaysTrue), alwaysTrue, [0, 1, 2, 3, 4, 5, 6, 7]);
    }

    [Fact]
    public void And_Complement()
    {
        AssertEquivalent(a.And(a.Not()), alwaysFalse, []);
    }

    [Fact]
    public void Or_Complement()
    {
        AssertEquivalent(a.Or(a.Not()), alwaysTrue, [0, 1, 2, 3, 4, 5, 6, 7]);
    }

    [Fact]
    public void And_Absorption()
    {
        AssertEquivalent(a.And(a.Or(b)), a, [1, 3, 5, 7]);
    }

    [Fact]
    public void Or_Absorption()
    {
        AssertEquivalent(a.Or(a.And(b)), a, [1, 3, 5, 7]);
    }

    private static void AssertEquivalent(Predicate left, Predicate right, int[] expected)
    {
        Assert.Equal(expected, items.Where(i => left).ToArray());
        Assert.Equal(expected, items.Where(i => right).ToArray());
    }
}
