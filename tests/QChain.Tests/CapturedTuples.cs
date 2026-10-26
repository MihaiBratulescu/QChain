using System.Linq.Expressions;

namespace QChain.Tests;

public class CapturedTuples
{
    public static TheoryData<(int, int)[]> TupleArrays => new()
    {
        Array.Empty<(int, int)>(),
        new[] { (1, 11), (3, 13) }
    };

    public static TheoryData<List<(int, int)>> TupleLists => new()
    {
        new List<(int, int)>(),
        new List<(int, int)> { (1, 11), (3, 13) }
    };

    [Fact]
    public void Members_In_Filter_And_Projection_Match_Ordinary_Linq()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        var captured = (Id: 2, Code: 12);
        Expression<Func<int, bool>> predicate = value => value == captured.Id;
        Expression<Func<int, ValueTuple<int, int>>> selector = value => ValueTuple.Create(value, captured.Code);

        var expected = source.Where(predicate).Select(selector).ToArray();
        var actual = source.AsQuery().Where(predicate).Select(selector).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Whole_Tuple_Projection_Composes_After_Distinct()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        var captured = (Id: 2, Code: 12);
        Expression<Func<int, ValueTuple<int, int>>> selector = value => captured;
        var expected = source.Select(selector).Distinct();
        var actual = source.AsQuery().Select(selector).Distinct();

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Where(pair => pair.Item1 == 2).Select(pair => pair.Item2).ToArray(),
            actual.Where(pair => pair.Item1 == 2).Select(pair => pair.Item2).ToArray());
    }

    [Fact]
    public void Equality_With_Query_Tuple_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        var captured = (2, 12);
        Expression<Func<ValueTuple<int, int>, bool>> predicate = pair => pair.Equals(captured);

        var expected = source.Select(value => ValueTuple.Create(value, value + 10)).Where(predicate).ToArray();
        var actual = source.AsQuery().Select(value => ValueTuple.Create(value, value + 10)).Where(predicate).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Large_Tuple_Projection_Preserves_All_Items()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        var captured = (1, 2, 3, 4, 5, 6, 7, 8);
        Expression<Func<int, (int, int, int, int, int, int, int, int)>> selector = value => captured;
       
        var expected = source.Select(selector).Distinct();
        var actual = source.AsQuery().Select(selector).Distinct();

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.Rest.Item1).ToArray(),
            actual.Select(pair => pair.Rest.Item1).ToArray());
    }

    [Theory]
    [MemberData(nameof(TupleArrays))]
    public void Array_Contains_Matches_Ordinary_Linq((int, int)[] captured)
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        Expression<Func<ValueTuple<int, int>, bool>> predicate = pair => captured.Contains(pair);

        var expected = source
            .Select(value => ValueTuple.Create(value, value + 10))
            .Where(predicate)
            .ToArray();

        var actual = source
            .AsQuery()
            .Select(value => ValueTuple.Create(value, value + 10))
            .Where(predicate)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TupleArrays))]
    public void Array_Enumerable_Contains_Matches_Ordinary_Linq((int, int)[] captured)
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        Expression<Func<ValueTuple<int, int>, bool>> predicate = pair => Enumerable.Contains(captured, pair);

        var expected = source.Select(value => ValueTuple.Create(value, value + 10)).Where(predicate).ToArray();
        var actual = source.AsQuery().Select(value => ValueTuple.Create(value, value + 10)).Where(predicate).ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TupleLists))]
    public void List_Contains_Matches_Ordinary_Linq(List<(int, int)> captured)
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        Expression<Func<ValueTuple<int, int>, bool>> predicate = pair => captured.Contains(pair);

        var expected = source.Select(value => ValueTuple.Create(value, value + 10)).Where(predicate).ToArray();
        var actual = source.AsQuery().Select(value => ValueTuple.Create(value, value + 10)).Where(predicate).ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TupleArrays))]
    public void Array_Any_Matches_Ordinary_Linq((int, int)[] captured)
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        Expression<Func<int, bool>> predicate = value => captured.Any(pair => pair.Item1 == value && pair.Item2 == value + 10);

        Assert.Equal(source.Where(predicate).ToArray(), source.AsQuery().Where(predicate).ToArray());
    }

    [Theory]
    [MemberData(nameof(TupleLists))]
    public void List_Any_Matches_Ordinary_Linq(List<(int, int)> captured)
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        Expression<Func<int, bool>> predicate = value => captured.Any(pair => pair.Item1 == value && pair.Item2 == value + 10);

        Assert.Equal(source.Where(predicate).ToArray(), source.AsQuery().Where(predicate).ToArray());
    }

    [Fact]
    public void Array_And_List_Indexing_Match_Ordinary_Linq()
    {
        var source = new[] { 0, 1 }.AsQueryable();
        (int, int)[] array = [(1, 11), (2, 12)];
        List<(int, int)> list = [(3, 13), (4, 14)];
        Expression<Func<int, ValueTuple<int, int>>> selector = index =>
            ValueTuple.Create(array[index].Item1, list[index].Item2);

        Assert.Equal(source.Select(selector).ToArray(), source.AsQuery().Select(selector).ToArray());
    }

    [Fact]
    public void Tuple_Changes_Are_Observed_Between_Executions()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        var captured = (Id: 1, Code: 11);
        Expression<Func<int, bool>> predicate = value => value == captured.Id;
        Expression<Func<int, ValueTuple<int, int>>> selector = value => captured;
        var expected = source.Where(predicate).Select(selector);
        var actual = source.AsQuery().Where(predicate).Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());

        captured = (3, 13);
        Assert.Equal(expected.ToArray(), actual.ToArray());

        captured.Code = 30;
        Assert.Equal(expected.ToArray(), actual.ToArray());
    }

    [Fact]
    public void Array_Changes_Are_Observed_Between_Executions()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        (int, int)[] captured = [(1, 11)];
        Expression<Func<ValueTuple<int, int>, bool>> predicate = pair => captured.Contains(pair);
        var expected = source.Select(value => ValueTuple.Create(value, value + 10)).Where(predicate);
        var actual = source.AsQuery().Select(value => ValueTuple.Create(value, value + 10)).Where(predicate);

        Assert.Equal(expected.ToArray(), actual.ToArray());

        captured[0] = (2, 12);
        Assert.Equal(expected.ToArray(), actual.ToArray());

        captured = [(3, 13)];
        Assert.Equal(expected.ToArray(), actual.ToArray());
    }

    [Fact]
    public void List_Changes_Are_Observed_Between_Executions()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        List<(int, int)> captured = [(1, 11)];
        Expression<Func<ValueTuple<int, int>, bool>> predicate = pair => captured.Contains(pair);
        var expected = source.Select(value => ValueTuple.Create(value, value + 10)).Where(predicate);
        var actual = source.AsQuery().Select(value => ValueTuple.Create(value, value + 10)).Where(predicate);

        Assert.Equal(expected.ToArray(), actual.ToArray());

        captured.Clear();
        captured.Add((2, 12));
        Assert.Equal(expected.ToArray(), actual.ToArray());

        captured = [(3, 13)];
        Assert.Equal(expected.ToArray(), actual.ToArray());
    }
}
