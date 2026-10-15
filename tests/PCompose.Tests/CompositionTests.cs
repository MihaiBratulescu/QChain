using System.Linq.Expressions;

namespace PCompose.Tests;

public class CompositionTests
{
    private static readonly IQueryable<int> items = Enumerable.Range(1, 10).AsQueryable();
    private static readonly Expression<Func<int, bool>> even = x => x % 2 == 0;
    private static readonly Expression<Func<int, bool>> multipleOfThree = x => x % 3 == 0;

    [Fact]
    public void And()
    {
        var result = items.Where(i => even.And(multipleOfThree)).ToArray();

        Assert.Equal([6], result);
    }

    [Fact]
    public void Or()
    {
        var result = items.Where(i => even.Or(multipleOfThree)).ToArray();

        Assert.Equal([2, 3, 4, 6, 8, 9, 10], result);
    }

    [Fact]
    public void Not()
    {
        var result = items.Where(i => even.Not()).ToArray();

        Assert.Equal([1, 3, 5, 7, 9], result);
    }

    [Fact]
    public void And_ExpressionAndPredicate()
    {
        var result = items.Where(i => even.And(multipleOfThree.Not())).ToArray();

        Assert.Equal([2, 4, 8, 10], result);
    }

    [Fact]
    public void And_PredicateAndExpression()
    {
        var result = items.Where(i => even.Not().And(multipleOfThree)).ToArray();

        Assert.Equal([3, 9], result);
    }

    [Fact]
    public void And_PredicateAndPredicate()
    {
        var result = items.Where(i => even.Not().And(multipleOfThree.Not())).ToArray();

        Assert.Equal([1, 5, 7], result);
    }

    [Fact]
    public void Or_ExpressionAndPredicate()
    {
        var result = items.Where(i => even.Or(multipleOfThree.Not())).ToArray();

        Assert.Equal([1, 2, 4, 5, 6, 7, 8, 10], result);
    }

    [Fact]
    public void Or_PredicateAndExpression()
    {
        var result = items.Where(i => even.Not().Or(multipleOfThree)).ToArray();

        Assert.Equal([1, 3, 5, 6, 7, 9], result);
    }

    [Fact]
    public void Or_PredicateAndPredicate()
    {
        var result = items.Where(i => even.Not().Or(multipleOfThree.Not())).ToArray();

        Assert.Equal([1, 2, 3, 4, 5, 7, 8, 9, 10], result);
    }

    [Fact]
    public void NestedComposition_PreservesGrouping()
    {
        Expression<Func<int, bool>> greaterThanTwo = value => value > 2;
        Expression<Func<int, bool>> isOne = value => value == 1;

        var result = items.Where(i => even.And(greaterThanTwo).Or(isOne).Not()).ToArray();

        Assert.Equal([2, 3, 5, 7, 9], result);
    }
}
