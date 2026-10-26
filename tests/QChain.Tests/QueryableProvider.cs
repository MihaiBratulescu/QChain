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
    public void Custom_Tuple_Conversion_Runs_Once_Per_Element()
    {
        var items = new[] { new CountingTupleConversion(), new CountingTupleConversion() };
        var source = items.AsQueryable();
        Expression<Func<CountingTupleConversion, ValueTuple<int, int>>> selector = value => (ValueTuple<int, int>)value;
        var expected = source.Select(selector).ToArray();
        var expectedCalls = items.Select(item => item.Calls).ToArray();
        Assert.All(expectedCalls, calls => Assert.Equal(1, calls));

        foreach (var item in items)
            item.Calls = 0;

        var actual = source.AsQuery().Select(selector).ToArray();

        Assert.Equal(expectedCalls, items.Select(item => item.Calls).ToArray());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Custom_Tuple_Conversion_Runs_Once_When_Used_By_A_Method()
    {
        var items = new[] { new CountingTupleConversion(), new CountingTupleConversion() };
        var source = items.AsQueryable();
        Expression<Func<CountingTupleConversion, bool>> selector = value =>
            ((ValueTuple<int, int>)value).Equals(ValueTuple.Create(1, 1));
        var expected = source.Select(selector).ToArray();
        var expectedCalls = items.Select(item => item.Calls).ToArray();
        Assert.All(expectedCalls, calls => Assert.Equal(1, calls));

        foreach (var item in items)
            item.Calls = 0;

        var actual = source.AsQuery().Select(selector).ToArray();

        Assert.Equal(expectedCalls, items.Select(item => item.Calls).ToArray());
        Assert.Equal(expected, actual);
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
    public void Nullable_Tuple_Value_Constructor_Matches_Ordinary_Linq()
    {
        var source = new[] { 0, 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>?>> selector = value =>
            new Nullable<ValueTuple<int, int>>(ValueTuple.Create(value, value + 10));
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.HasValue).ToArray(), actual.Select(pair => pair.HasValue).ToArray());
    }

    [Fact]
    public void Nullable_Tuple_Empty_Constructor_Matches_Ordinary_Linq()
    {
        var source = new[] { 0, 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>?>> selector = value => new Nullable<ValueTuple<int, int>>();
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.HasValue).ToArray(), actual.Select(pair => pair.HasValue).ToArray());
    }

    [Fact]
    public void Nullable_Tuple_As_Cast_Matches_Ordinary_Linq()
    {
        var source = new object?[] { null, (1, 2), "not a tuple", (3, 4) }.AsQueryable();
        Expression<Func<object?, ValueTuple<int, int>?>> selector = value => value as ValueTuple<int, int>?;
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Where(pair => pair.HasValue).Select(pair => pair!.Value.Item1).ToArray(),
            actual.Where(pair => pair.HasValue).Select(pair => pair!.Value.Item1).ToArray());
    }

    [Fact]
    public void Tuple_Identity_Conversion_Matches_Ordinary_Linq()
    {
        var source = new[] { (1, 11), (2, 12) }.AsQueryable();
        var pair = Expression.Parameter(typeof(ValueTuple<int, int>), "pair");
        var selector = Expression.Lambda<Func<ValueTuple<int, int>, ValueTuple<int, int>>>(
            Expression.Convert(pair, pair.Type), pair);
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(value => value.Item2).ToArray(), actual.Select(value => value.Item2).ToArray());
    }

    [Fact]
    public void Nullable_Tuple_Identity_Conversion_Matches_Ordinary_Linq()
    {
        var source = new (int, int)?[] { null, (0, 0), (1, 2) }.AsQueryable();
        var pair = Expression.Parameter(typeof(ValueTuple<int, int>?), "pair");
        var selector = Expression.Lambda<Func<ValueTuple<int, int>?, ValueTuple<int, int>?>>(
            Expression.Convert(pair, pair.Type), pair);
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(value => value.HasValue).ToArray(), actual.Select(value => value.HasValue).ToArray());
    }

    [Fact]
    public void Tuple_With_Empty_Rest_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int, int, int, int, int, int, ValueTuple>>> selector = value =>
            new ValueTuple<int, int, int, int, int, int, int, ValueTuple>(
                value, value + 1, value + 2, value + 3, value + 4, value + 5, value + 6, new ValueTuple());
        var expected = source.Select(selector).Distinct();
        var actual = source.AsQuery().Select(selector).Distinct();

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.Rest).ToArray(), actual.Select(pair => pair.Rest).ToArray());
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
    public void Nullable_Tuple_Coalesce_With_Conversion_Matches_Ordinary_Linq()
    {
        var source = new (int, int)?[] { null, (0, 0), (1, 2) }.AsQueryable();
        var item = Expression.Parameter(typeof(ValueTuple<int, int>?), "item");
        Expression<Func<ValueTuple<int, int>, ValueTuple<int, int>?>> conversion = pair =>
            ValueTuple.Create(pair.Item1 + 100, pair.Item2 + 10);
        var fallback = Expression.Convert(Expression.Constant(ValueTuple.Create(9, 9)), typeof(ValueTuple<int, int>?));
        var selector = Expression.Lambda<Func<ValueTuple<int, int>?, ValueTuple<int, int>?>>(
            Expression.Coalesce(item, fallback, conversion), item);

        var expected = source.Select(selector).ToArray();
        var actual = source.AsQuery().Select(selector).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Nullable_Tuple_Coalesce_Conversion_Accepts_The_Whole_Nullable_Value()
    {
        var source = new (int, int)?[] { null, (0, 0), (1, 2) }.AsQueryable();
        var item = Expression.Parameter(typeof(ValueTuple<int, int>?), "item");
        Expression<Func<ValueTuple<int, int>?, int>> conversion = pair => pair!.Value.Item1 + 100;
        var selector = Expression.Lambda<Func<ValueTuple<int, int>?, int>>(
            Expression.Coalesce(item, Expression.Constant(-1), conversion), item);

        var expected = source.Select(selector).ToArray();
        var actual = source.AsQuery().Select(selector).ToArray();

        Assert.Equal(expected, actual);
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
    public void Enumerable_DefaultIfEmpty_Preserves_Component_Defaults_Inside_Selector()
    {
        var source = new (int, string?)[][]
        {
            [(1, "one")],
            [],
            [(0, "zero")]
        }.AsQueryable();
        var expected = source.SelectMany(group => group.DefaultIfEmpty()).ToArray();
        var actual = source.AsQuery().SelectMany(group => group.DefaultIfEmpty()).ToArray();

        Assert.Equal([(1, "one"), (0, (string?)null), (0, "zero")], actual);
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
    public void Immediate_Anonymous_Member_Access_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable();
        Expression<Func<int, ValueTuple<int, int>>> selector = value =>
            new { Other = value + 99, Pair = ValueTuple.Create(value, value + 10) }.Pair;
        var expected = source.Select(selector).Distinct();
        var actual = source.AsQuery().Select(selector).Distinct();

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Where(pair => pair.Item1 > 1).Select(pair => pair.Item2).ToArray(),
            actual.Where(pair => pair.Item1 > 1).Select(pair => pair.Item2).ToArray());
    }

    [Fact]
    public void Dto_Tuple_Assignment_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ProjectionContainer>> selector = value => new ProjectionContainer
        {
            Pair = ValueTuple.Create(value, value + 10)
        };
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.Select(item => item.Pair).ToArray(), actual.Select(item => item.Pair).ToArray());
        Assert.Equal(expected.Select(item => item.Unassigned).ToArray(), actual.Select(item => item.Unassigned).ToArray());
    }

    [Fact]
    public void Dto_Unassigned_Member_Preserves_Its_Initializer()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, int>> selector = value => new ProjectionContainer
        {
            Pair = ValueTuple.Create(value, value + 10)
        }.Unassigned;

        Assert.Equal(source.Select(selector).ToArray(), source.AsQuery().Select(selector).ToArray());
    }

    [Fact]
    public void Nested_Object_Tuple_Binding_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable();
        Expression<Func<int, ProjectionContainer>> selector = value => new ProjectionContainer
        {
            Child = { Pair = ValueTuple.Create(value, value + 10) }
        };
        var expected = source.Select(selector).Select(item => item.Child.Pair).Distinct().ToArray();
        var actual = source.AsQuery().Select(selector).Select(item => item.Child.Pair).Distinct().ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Collection_Property_Tuple_Binding_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        Expression<Func<int, ProjectionContainer>> selector = value => new ProjectionContainer
        {
            Items = { ValueTuple.Create(value, value + 10), ValueTuple.Create(value + 1, value + 11) }
        };
        var expected = source.Select(selector).SelectMany(item => item.Items).ToArray();
        var actual = source.AsQuery().Select(selector).SelectMany(item => item.Items).ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Explicit_List_Index_Rewrites_Collection_And_Result(int index)
    {
        var source = new[]
        {
            new List<(int, int)> { (1, 11), (2, 12) },
            new List<(int, int)> { (3, 13), (4, 14) }
        }.AsQueryable();
        var items = Expression.Parameter(typeof(List<ValueTuple<int, int>>), "items");
        var access = Expression.MakeIndex(items, items.Type.GetProperty("Item"), [Expression.Constant(index)]);
        var selector = Expression.Lambda<Func<List<ValueTuple<int, int>>, ValueTuple<int, int>>>(access, items);
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.Item2).ToArray(), actual.Select(pair => pair.Item2).ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Explicit_Array_Index_Rewrites_Collection_And_Result(int index)
    {
        var source = new[]
        {
            new[] { (1, 11), (2, 12) },
            new[] { (3, 13), (4, 14) }
        }.AsQueryable();
        var items = Expression.Parameter(typeof(ValueTuple<int, int>[]), "items");
        var access = Expression.MakeIndex(items, null, [Expression.Constant(index)]);
        var selector = Expression.Lambda<Func<ValueTuple<int, int>[], ValueTuple<int, int>>>(access, items);
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.Item2).ToArray(), actual.Select(pair => pair.Item2).ToArray());
    }

    [Fact]
    public void Explicit_Tuple_Index_Argument_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        var lookup = new TupleLookup();
        var value = Expression.Parameter(typeof(int), "value");
        var key = Expression.New(typeof(ValueTuple<int, int>).GetConstructor([typeof(int), typeof(int)])!,
            value, Expression.Add(value, Expression.Constant(10)));
        var access = Expression.MakeIndex(Expression.Constant(lookup), typeof(TupleLookup).GetProperty("Item"), [key]);
        var selector = Expression.Lambda<Func<int, int>>(access, value);

        Assert.Equal(source.Select(selector).ToArray(), source.AsQuery().Select(selector).ToArray());
    }

    [Fact]
    public void Explicit_Tuple_Index_Result_Matches_Ordinary_Linq()
    {
        var source = new[] { 1, 2 }.AsQueryable();
        var lookup = new TupleValueLookup();
        var value = Expression.Parameter(typeof(int), "value");
        var access = Expression.MakeIndex(Expression.Constant(lookup), typeof(TupleValueLookup).GetProperty("Item"), [value]);
        var selector = Expression.Lambda<Func<int, ValueTuple<int, int>>>(access, value);
        var expected = source.Select(selector);
        var actual = source.AsQuery().Select(selector);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Where(pair => pair.Item1 > 1).Select(pair => pair.Item2).ToArray(),
            actual.Where(pair => pair.Item1 > 1).Select(pair => pair.Item2).ToArray());
    }

    [Fact]
    public void Grouping_NonGeneric_Enumeration_Exposes_Public_Tuples()
    {
        var source = new[] { 1, 1, 2 }.AsQueryable();
        var expected = source.Select(value => ValueTuple.Create(value, value + 10))
            .GroupBy(pair => ValueTuple.Create(pair.Item1 % 2)).OrderBy(group => group.Key.Item1).ToArray();
        var actual = source.AsQuery().Select(value => ValueTuple.Create(value, value + 10))
            .GroupBy(pair => ValueTuple.Create(pair.Item1 % 2)).OrderBy(group => group.Key.Item1).ToArray();

        Assert.Equal(expected.Select(group => group.Key).ToArray(), actual.Select(group => group.Key).ToArray());
        for (var index = 0; index < expected.Length; index++)
        {
            var expectedItems = ((IEnumerable)expected[index]).Cast<object>().ToArray();
            var actualItems = ((IEnumerable)actual[index]).Cast<object>().ToArray();
            Assert.Equal(expectedItems, actualItems);
            Assert.All(actualItems, item => Assert.IsType<ValueTuple<int, int>>(item));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Constant_Query_Uses_Its_Underlying_Expression(int minimumId)
    {
        var source = new[] { (1, 11), (2, 12), (3, 13) }.AsQueryable();
        var query = new ExpressionQuery<ValueTuple<int, int>>(source.Where(pair => pair.Item1 > minimumId));
        var expression = Expression.Constant(query, typeof(IQueryable<ValueTuple<int, int>>));
        var expected = source.Provider.CreateQuery<ValueTuple<int, int>>(expression);
        var actual = source.AsQuery().Provider.CreateQuery<ValueTuple<int, int>>(expression);

        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.Equal(expected.Select(pair => pair.Item2).ToArray(), actual.Select(pair => pair.Item2).ToArray());
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

    private sealed class ExpressionQuery<T>(IQueryable<T> source) : IQueryable<T>
    {
        public Type ElementType => typeof(T);
        public Expression Expression => source.Expression;
        public IQueryProvider Provider => source.Provider;
        public IEnumerator<T> GetEnumerator() => source.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class CountingTupleConversion
    {
        public int Calls { get; set; }

        public static implicit operator ValueTuple<int, int>(CountingTupleConversion value)
        {
            var call = ++value.Calls;
            return (call, call);
        }
    }

    private sealed class ProjectionContainer
    {
        public (int, int) Pair { get; set; }
        public int Unassigned { get; set; } = 42;
        public ProjectionChild Child { get; } = new();
        public List<(int, int)> Items { get; } = [];
    }

    private sealed class ProjectionChild
    {
        public (int, int) Pair { get; set; }
    }

    private sealed class TupleLookup
    {
        public int this[(int Id, int Code) pair] => pair.Id + pair.Code;
    }

    private sealed class TupleValueLookup
    {
        public (int, int) this[int value] => (value, value + 10);
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
