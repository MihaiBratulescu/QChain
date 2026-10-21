using QChain.Internal.Helpers;
using QChain.Internal.Types;
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
        Assert.True(CarrierProjection.IsCarrier(shape.Source.ElementType));
        Assert.Equal(arity, shape.Source.ElementType.GetGenericArguments().Length);
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
    public void Chained_Joins_Pack_Beyond_Seven_Items_And_Preserve_Branches()
    {
        var source = new[] { 1, 2 }.AsQueryable().AsQuery();
        var first = source.Join(source, x => x, x => x, (a, b) => ValueTuple.Create(a, b));
        var seven = first
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, a.Item4, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, b))
            .Join(source, x => x.Item1, x => x, (a, b) => ValueTuple.Create(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6, b));

        Assert.Equal(7, ((IShapedQuery)seven).QueryShape.Source.ElementType.GetGenericArguments().Length);
        Assert.Equal([(1, 1, 1, 1, 1, 1, 1), (2, 2, 2, 2, 2, 2, 2)], seven.ToArray());
        Assert.Equal([(1, 1), (2, 2)], first.ToArray());
        Assert.Equal([1, 2], source.ToArray());
        var eight = seven.Join(source, x => x.Item1, x => x, (a, b) =>
            ValueTuple.Create(a.Item1, a.Item2, a.Item3, a.Item4, a.Item5, a.Item6, a.Item7, b * 10));
        Assert.Equal([ValueTuple.Create(1, 1, 1, 1, 1, 1, 1, 10), ValueTuple.Create(2, 2, 2, 2, 2, 2, 2, 20)], eight.ToArray());
        Assert.Equal([20], eight.Where(x => x.Rest.Item1 > 10).Select(x => x.Rest.Item1).ToArray());
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
    public void Empty_Tuple_Create_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple>> selector = value => ValueTuple.Create();

        var expected = source.Select(selector).Distinct().ToArray();
        var actual = source.AsQuery().Select(selector).Distinct().ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Default_Tuple_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>>> selector = value => default(ValueTuple<int, int>);

        var expected = source.Select(selector).Distinct().ToArray();
        var actual = source.AsQuery().Select(selector).Distinct().ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void New_Tuple_Array_Preserves_Component_Defaults(int length)
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>[]>> selector = value => new ValueTuple<int, int>[length];
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        var expectedRows = expected.ToArray();
        var actualRows = actual.ToArray();
        Assert.Equal(expectedRows.Length, actualRows.Length);
        for (var index = 0; index < expectedRows.Length; index++)
            Assert.Equal(expectedRows[index], actualRows[index]);

        Assert.Equal(expected.SelectMany(items => items).Select(item => item.Item1).ToArray(),
            actual.SelectMany(items => items).Select(item => item.Item1).ToArray());
    }

    [Fact]
    public void Tuple_Member_Initializer_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>>> selector = value => new ValueTuple<int, int>
        {
            Item1 = value,
            Item2 = value + 10
        };
        var expected = source.Select(selector).Distinct();
        var actual = source.AsQuery().Select(selector).Distinct();

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Where(pair => pair.Item1 > 1).Select(pair => pair.Item2).ToArray(),
            actual.Where(pair => pair.Item1 > 1).Select(pair => pair.Item2).ToArray());
    }

    [Fact]
    public void Tuple_Partial_Initializer_Preserves_Unassigned_Defaults()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>>> selector = value => new ValueTuple<int, int> { Item2 = value + 10 };
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.Item1).ToArray(), actual.Select(pair => pair.Item1).ToArray());
    }

    [Fact]
    public void Tuple_Initializer_Overrides_Constructor_Values()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>>> selector = value => new ValueTuple<int, int>(value, 99)
        {
            Item2 = value + 10
        };

        Assert.Equal(source.Select(selector).ToArray(), source.AsQuery().Select(selector).ToArray());
    }

    [Fact]
    public void Nested_Tuple_Initializer_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, ValueTuple<int, int>>>> selector = value => new ValueTuple<int, ValueTuple<int, int>>
        {
            Item1 = value,
            Item2 = new ValueTuple<int, int> { Item1 = value + 10, Item2 = value + 20 }
        };
        var expected = source.Select(selector).Distinct();
        var actual = source.AsQuery().Select(selector).Distinct();

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Where(pair => pair.Item2.Item1 > 11).Select(pair => pair.Item2.Item2).ToArray(),
            actual.Where(pair => pair.Item2.Item1 > 11).Select(pair => pair.Item2.Item2).ToArray());
    }

    [Fact]
    public void Large_Tuple_Rest_Initializer_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable();
        var value = Expression.Parameter(typeof(int), "value");
        var tupleType = typeof(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int>>);
        var restType = typeof(ValueTuple<int>);
        var initializer = Expression.MemberInit(Expression.New(tupleType),
            Expression.Bind(tupleType.GetField("Item1")!, value),
            Expression.Bind(tupleType.GetField("Item7")!, Expression.Add(value, Expression.Constant(6))),
            Expression.Bind(tupleType.GetField("Rest")!, Expression.MemberInit(Expression.New(restType),
                Expression.Bind(restType.GetField("Item1")!, Expression.Add(value, Expression.Constant(7))))));
        var selector = Expression.Lambda<Func<int, (int, int, int, int, int, int, int, int)>>(initializer, value);
        var expected = source.Select(selector).Distinct();
        var actual = source.AsQuery().Select(selector).Distinct();

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Where(pair => pair.Rest.Item1 > 8).Select(pair => pair.Item7).ToArray(),
            actual.Where(pair => pair.Rest.Item1 > 8).Select(pair => pair.Item7).ToArray());
    }

    [Fact]
    public void Tuple_Array_Initializer_And_Indexing_Match_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>[]>> selector = value => new[]
        {
            new ValueTuple<int, int> { Item1 = value, Item2 = value + 10 },
            ValueTuple.Create(value + 1, value + 11)
        };
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(items => items[1].Item2).ToArray(),
            actual.Select(items => items[1].Item2).ToArray());
    }

    [Fact]
    public void Tuple_List_Initializer_And_Indexing_Match_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, List<ValueTuple<int, int>>>> selector = value => new List<ValueTuple<int, int>>
        {
            new ValueTuple<int, int> { Item1 = value, Item2 = value + 10 },
            ValueTuple.Create(value + 1, value + 11)
        };
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(items => items[1].Item2).ToArray(),
            actual.Select(items => items[1].Item2).ToArray());
    }

    [Fact]
    public void Nullable_Tuple_Preserves_Present_And_Absent_Values()
    {
        var source = new[] { 0, 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>?>> selector = value => value == 0
            ? (ValueTuple<int, int>?)null : ValueTuple.Create(value, value + 10);
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(value => value.HasValue).ToArray(),
            actual.Select(value => value.HasValue).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nullable_Tuple_Value_Matches_Ordinary_Linq(bool hasValue)
    {
        var source = new (int, int)?[] { hasValue ? (1, 2) : null }.AsQueryable();
        Expression<Func<ValueTuple<int, int>?, ValueTuple<int, int>>> selector = value => value!.Value;
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        if (hasValue)
            Assert.Equal(expected.ToArray(), actual.ToArray());
        else
        {
            var expectedError = Assert.Throws<InvalidOperationException>(() => expected.ToArray());
            var actualError = Assert.Throws<InvalidOperationException>(() => actual.ToArray());
            Assert.Equal(expectedError.Message, actualError.Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nullable_Tuple_Cast_Matches_Ordinary_Linq(bool hasValue)
    {
        var source = new (int, int)?[] { hasValue ? (1, 2) : null }.AsQueryable();
        Expression<Func<ValueTuple<int, int>?, ValueTuple<int, int>>> selector = value => (ValueTuple<int, int>)value!;
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        if (hasValue)
            Assert.Equal(expected.ToArray(), actual.ToArray());
        else
        {
            var expectedError = Assert.Throws<InvalidOperationException>(() => expected.ToArray());
            var actualError = Assert.Throws<InvalidOperationException>(() => actual.ToArray());
            Assert.Equal(expectedError.Message, actualError.Message);
        }
    }

    [Fact]
    public void Boxed_Nullable_Tuple_Conversion_Matches_Ordinary_Linq()
    {
        var source = new object?[] { null, (1, 2) }.AsQueryable();
        Expression<Func<object?, ValueTuple<int, int>?>> selector = value => (ValueTuple<int, int>?)value;

        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(value => value.HasValue).ToArray(),
            actual.Select(value => value.HasValue).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Captured_Nullable_Tuple_Value_Preserves_Check_After_Composition(bool hasValue)
    {
        var source = new[] { 1 }.AsQueryable();
        (int, int)? captured = hasValue ? (1, 2) : null;
        Expression<Func<int, ValueTuple<int, int>>> selector = value => captured!.Value;
        var expected = source.Select(selector).Select(value => value.Item1);
        var actual = source.AsQuery().Select(selector).Select(value => value.Item1);

        if (hasValue)
            Assert.Equal(expected.ToArray(), actual.ToArray());
        else
        {
            var expectedError = Assert.Throws<InvalidOperationException>(() => expected.ToArray());
            var actualError = Assert.Throws<InvalidOperationException>(() => actual.ToArray());
            Assert.Equal(expectedError.Message, actualError.Message);
        }
    }

    [Fact]
    public void Nullable_Tuple_GetValueOrDefault_Matches_Ordinary_Linq()
    {
        var source = new (int, int)?[] { null, (0, 0), (1, 2) }.AsQueryable();
        Expression<Func<ValueTuple<int, int>?, ValueTuple<int, int>>> selector = value => value.GetValueOrDefault();

        Assert.Equal(source.Select(selector).ToArray(), source.AsQuery().Select(selector).ToArray());
    }

    [Fact]
    public void Nullable_Tuple_GetValueOrDefault_With_Fallback_Matches_Ordinary_Linq()
    {
        var source = new (int, int)?[] { null, (0, 0), (1, 2) }.AsQueryable();
        Expression<Func<ValueTuple<int, int>?, ValueTuple<int, int>>> selector = value =>
            value.GetValueOrDefault(ValueTuple.Create(3, 4));

        Assert.Equal(source.Select(selector).ToArray(), source.AsQuery().Select(selector).ToArray());
    }

    [Fact]
    public void Nullable_Tuple_Coalesce_Matches_Ordinary_Linq()
    {
        var source = new (int, int)?[] { null, (0, 0), (1, 2) }.AsQueryable();
        Expression<Func<ValueTuple<int, int>?, ValueTuple<int, int>>> selector = value => value ?? ValueTuple.Create(3, 4);

        Assert.Equal(source.Select(selector).ToArray(), source.AsQuery().Select(selector).ToArray());
    }

    [Fact]
    public void Nullable_Tuple_Guarded_Access_Matches_Ordinary_Linq()
    {
        var source = new (int, int)?[] { null, (0, 0), (1, 2) }.AsQueryable();
        Expression<Func<ValueTuple<int, int>?, bool>> predicate = value => value.HasValue && value.Value.Item1 > 0;

        var expected = source.Where(predicate).Select(value => value!.Value).ToArray();
        var actual = source.AsQuery().Where(predicate).Select(value => value!.Value).ToArray();

        Assert.Equal(expected, actual);
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

    [Fact]
    public void NonGeneric_CreateQuery_Preserves_Original_Provider_Exception()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        var expected = new NotSupportedException("The provider rejected this query.");
        IQueryProvider provider = new QueryProvider(new ThrowingProvider(expected), new QueryShape<int>(source));
        var expression = source.Where(value => value > 1).Expression;

        var error = Assert.Throws<NotSupportedException>(() => provider.CreateQuery(expression));

        Assert.Same(expected, error);
        Assert.Contains(nameof(ThrowingProvider.CreateQuery), error.StackTrace);
    }

    private sealed class ThrowingProvider(Exception error) : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => throw error;
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => throw error;
        public object? Execute(Expression expression) => throw error;
        public TResult Execute<TResult>(Expression expression) => throw error;
    }

    private static readonly Type[] TupleDefinitions =
    [
        typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
        typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>)
    ];
}
