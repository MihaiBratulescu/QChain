using System.Linq.Expressions;

namespace PCompose.Tests;

public class DirectPredicateTests
{
    [Theory]
    [InlineData(new int[] { }, new int[] { })]
    [InlineData(new int[] { 1, 3, 5 }, new int[] { })]
    [InlineData(new int[] { 2, 4, 6 }, new int[] { 2, 4, 6 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5, 6 }, new int[] { 2, 4, 6 })]
    public void Compile_FiltersQueryable(int[] values, int[] expected)
    {
        Expression<Func<int, bool>> even = value => value % 2 == 0;
        Predicate predicate = even;

        var result = values.AsQueryable()
            .Where(predicate.Compile<int>())
            .ToArray();

        Assert.Equal(expected, result);
    }
}
