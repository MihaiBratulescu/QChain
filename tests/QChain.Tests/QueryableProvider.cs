using QChain.Internal.Helpers;
using System.Collections;
using System.Linq.Expressions;

namespace QChain.Tests;

public class QueryableProvider
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Tuple_Distinct_Uses_Matching_Carrier_Arity(int arity)
    {
        var source = new[] { 1, 1, 2 }.AsQueryable().AsQuery();
        var item = Expression.Parameter(typeof(int), "item");
        var tupleType = TupleDefinitions[arity - 1].MakeGenericType(Enumerable.Repeat(typeof(int), arity).ToArray());
        var tuple = Expression.New(tupleType.GetConstructors().Single(), Enumerable.Repeat(item, arity));
        var projected = source.Provider.CreateQuery(Expression.Call(typeof(Queryable), nameof(Queryable.Select),
            [typeof(int), tupleType], source.Expression, Expression.Quote(Expression.Lambda(tuple, item))));
        var distinct = projected.Provider.CreateQuery(Expression.Call(typeof(Queryable), nameof(Queryable.Distinct),
            [tupleType], projected.Expression));

        var shape = ((IShapedQuery)distinct).QueryShape;
        Assert.True(CarrierProjection.IsCarrier(shape.SourceType));
        Assert.Equal(arity, shape.SourceType.GetGenericArguments().Length);
        var rows = ((IEnumerable)distinct).Cast<object>().ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(2, tupleType.GetField($"Item{arity}")!.GetValue(rows[1]));
    }

    [Fact]
    public void Set_Operations_Compare_Tuple_Values_And_Preserve_Each_Projection()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable().AsQuery();
        var left = source.Select(x => ValueTuple.Create(x, x + 10));
        var right = source.Where(x => x < 3).Select(x => ValueTuple.Create(x + 1, x + 11));

        Assert.Equal([(1, 11), (2, 12), (3, 13)], left.Union(right).OrderBy(x => x.Item1));
        Assert.Equal([(1, 11)], left.Except(right));
        Assert.Equal([(2, 12), (3, 13)], left.Intersect(right));
        Assert.Equal(5, left.Concat(right).Count());
        Assert.Equal([2, 3], left.IntersectBy([2, 3], x => x.Item1).Select(x => x.Item1));
        Assert.Equal([1], left.ExceptBy([2, 3], x => x.Item1).Select(x => x.Item1));
    }

    [Fact]
    public void Nested_Tuple_In_Anonymous_Projection_Composes_After_Distinct()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable().AsQuery();
        var rows = source.Select(x => new { Value = ValueTuple.Create(x, ValueTuple.Create(x + 1)), Label = x })
            .Distinct().Where(x => x.Value.Item2.Item1 > 2)
            .Select(x => ValueTuple.Create(x.Label, x.Value.Item1)).ToArray();

        Assert.Equal([(2, 2)], rows);
    }

    [Fact]
    public void Chained_Joins_Flatten_Through_Seven_Items_And_Preserve_Branches()
    {
        var source = new[] { 1, 2 }.AsQueryable().AsQuery();
        var first = source.Join(source, x => x, x => x, (a, b) => ValueTuple.Create(a, b));
        var seven = first
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, a.Item4, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6, b));

        Assert.Equal(7, ((IShapedQuery)seven).QueryShape.SourceType.GetGenericArguments().Length);
        Assert.Equal([(1, 1, 1, 1, 1, 1, 1), (2, 2, 2, 2, 2, 2, 2)], seven.ToArray());
        Assert.Equal([(1, 1), (2, 2)], first.ToArray());
        Assert.Equal([1, 2], source.ToArray());
        Assert.Equal(2, seven.Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a, b)).Count());
    }

    [Fact]
    public void Terminals_Translate_Predicates_And_Aggregates()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable().AsQuery()
            .Select(x => ValueTuple.Create(x, x * 10));

        Assert.Equal(2, source.Count(x => x.Item1 > 1));
        Assert.Equal(2L, source.LongCount(x => x.Item1 > 1));
        Assert.True(source.Any(x => x.Item2 == 20));
        Assert.True(source.All(x => x.Item2 > 0));
        Assert.Equal((2, 20), source.First(x => x.Item1 > 1));
        Assert.Equal((3, 30), source.Last(x => x.Item1 > 1));
        Assert.Equal((2, 20), source.Single(x => x.Item1 == 2));
        Assert.Equal((3, 30), source.FirstOrDefault(x => x.Item1 > 10, (3, 30)));
        Assert.Equal(60, source.Sum(x => x.Item2));
        Assert.Equal(20, source.Average(x => x.Item2));
        Assert.Equal(10, source.Min(x => x.Item2));
        Assert.Equal(30, source.Max(x => x.Item2));
        Assert.Equal((2, 20), source.ElementAt(1));
        Assert.Contains((2, 20), source);
        Assert.Throws<InvalidOperationException>(() => source.Single());
    }

    [Fact]
    public void DefaultIfEmpty_Rebuilds_Empty_And_Present_Tuples()
    {
        var source = Array.Empty<int>().AsQueryable().AsQuery();
        Assert.Equal([(0, false, (string?)null)], source.Select(x => ValueTuple.Create(x, true, (string?)"present")).DefaultIfEmpty().ToArray());
        Assert.Equal(["missing"], source.Select(x => ValueTuple.Create(x, "present"))
            .DefaultIfEmpty().Where(x => x.Item1 == 0).Select(x => x.Item2 ?? "missing").ToArray());
        Assert.Null(source.Select(x => (ValueTuple<int, bool>?)ValueTuple.Create(x, true)).DefaultIfEmpty().Single());
        Assert.Equal([(1, true)], new[] { 1 }.AsQueryable().AsQuery().Select(x => ValueTuple.Create(x, true)).DefaultIfEmpty().ToArray());
    }

    [Fact]
    public void SelectMany_Handles_Collection_Methods_And_Projected_Groups()
    {
        var source = new[] { 1, 2 }.AsQueryable().AsQuery();
        Assert.Equal([1, 1, 2, 2], source.SelectMany(x => Enumerable.Repeat(x, 2)).ToArray());
        Assert.Equal([(1, 0), (2, 0), (2, 1)], source.SelectMany(x => Enumerable.Range(0, x)
            .Select(item => ValueTuple.Create(x, item))).ToArray());
        Assert.Equal([1, 2, 3], new[] { new[] { 1, 2 }, new[] { 3 } }.AsQueryable().AsQuery().SelectMany(x => x).ToArray());
        var grouped = source.GroupJoin(source.Select(x => ValueTuple.Create(x, x + 10)),
            x => x, x => x.Item1, (x, rows) => new { Value = x, Rows = rows });
        Assert.Equal([(1, 11), (2, 12)], grouped.SelectMany(x => x.Rows).ToArray());
        Assert.Equal([(1, 11), (2, 12)], grouped.SelectMany(x => x.Rows, (x, row) => ValueTuple.Create(x.Value, row.Item2)).ToArray());
        Assert.Equal([0, 0], source.GroupJoin(source.Where(x => x > 5), x => x, x => x, (x, rows) => rows)
            .SelectMany(x => x.DefaultIfEmpty()).ToArray());
    }

    [Fact]
    public void Provider_Can_Create_And_Execute_From_Existing_Expressions()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable().AsQuery();
        var filtered = source.Where(x => x > 1);
        Assert.Equal([2, 3], source.Provider.CreateQuery<int>(filtered.Expression));
        Assert.Equal([2, 3], source.Provider.Execute<IEnumerable<int>>(filtered.Expression));
        var terminal = Expression.Call(typeof(Queryable), nameof(Queryable.Count), [typeof(int)], filtered.Expression);
        Assert.Equal(2, source.Provider.Execute(terminal));
        Assert.Same(source, source.AsQuery());
    }

    private static readonly Type[] TupleDefinitions =
    [
        typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
        typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>)
    ];
}
