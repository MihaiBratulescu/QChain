using Microsoft.EntityFrameworkCore;
using QChain.Internal.Helpers;
using QChain.Tests.TestBench;
using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace QChain.Tests;

public class LargeTuples(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Theory]
    [InlineData(8, 2)]
    [InlineData(9, 2)]
    [InlineData(13, 2)]
    [InlineData(14, 2)]
    [InlineData(15, 3)]
    [InlineData(48, 7)]
    [InlineData(49, 7)]
    [InlineData(50, 2)]
    public void Packing_Preserves_All_Items_And_Caps_Each_Carrier(int arity, int rootArity)
    {
        var query = Project(new[] { 1, 1, 2 }.AsQueryable().AsQuery(), arity);
        var carrier = ((IShapedQuery)query).QueryShape.Source.ElementType;
        Assert.Equal(rootArity, carrier.GetGenericArguments().Length);
        AssertCarrierArities(carrier);
        if (arity == 14)
            Assert.All(carrier.GetGenericArguments(), group => Assert.Equal(7, group.GetGenericArguments().Length));
        if (arity == 50)
        {
            var groups = carrier.GetGenericArguments();
            Assert.Equal(typeof(int), groups[1]);
            Assert.Equal(7, groups[0].GetGenericArguments().Length);
            Assert.All(groups[0].GetGenericArguments(), group => Assert.Equal(7, group.GetGenericArguments().Length));
        }

        query = Apply(query, nameof(Queryable.Distinct), [query.ElementType]);
        var row = Expression.Parameter(query.ElementType, "row");
        query = Apply(query, nameof(Queryable.OrderByDescending), [query.ElementType, typeof(int)],
            Expression.Quote(Expression.Lambda(Item(row, arity - 1), row)));
        AssertRows(query, arity, [2, 1]);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(49)]
    [InlineData(50)]
    public void Sqlite_Distinct_Filter_And_Order_Access_Items_Across_Groups(int arity)
    {
        var query = Project(_fixture.db.Accounts.Select(account => account.AccountId), arity);
        query = Apply(query, nameof(Queryable.Distinct), [query.ElementType]);
        var row = Expression.Parameter(query.ElementType, "row");
        var last = Item(row, arity - 1);
        query = Apply(query, nameof(Queryable.Where), [query.ElementType],
            Expression.Quote(Expression.Lambda(Expression.GreaterThan(last, Expression.Constant(100 + arity - 1)), row)));
        query = Apply(query, nameof(Queryable.OrderByDescending), [query.ElementType, typeof(int)],
            Expression.Quote(Expression.Lambda(last, row)));

        Assert.Contains("WHERE", query.ToQueryString());
        AssertRows(query, arity, [7, 6, 5, 4, 3, 2]);
        Assert.Equal(6, query.Provider.Execute<int>(Expression.Call(typeof(Queryable), nameof(Queryable.Count),
            [query.ElementType], query.Expression)));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    [InlineData(50)]
    public void DefaultIfEmpty_Preserves_Every_Component_Default(int arity)
    {
        var query = Project(Array.Empty<int>().AsQueryable().AsQuery(), arity);
        query = Apply(query, nameof(Queryable.DefaultIfEmpty), [query.ElementType]);
        var row = Assert.Single(((IEnumerable)query).Cast<ITuple>());
        Assert.Equal(arity, row.Length);
        Assert.All(Enumerable.Range(0, arity), index => Assert.Equal(0, row[index]));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    public void Sqlite_DefaultIfEmpty_Preserves_Every_Component_Default(int arity)
    {
        var query = Project(_fixture.db.Accounts.Where(account => account.AccountId < 0)
            .Select(account => account.AccountId), arity);
        query = Apply(query, nameof(Queryable.DefaultIfEmpty), [query.ElementType]);
        var row = Assert.Single(((IEnumerable)query).Cast<ITuple>());
        Assert.Equal(arity, row.Length);
        Assert.All(Enumerable.Range(0, arity), index => Assert.Equal(0, row[index]));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    public void Nullable_Empty_Tuple_Remains_Null(int arity)
    {
        var query = Project(Array.Empty<int>().AsQueryable().AsQuery(), arity, nullable: true);
        query = Apply(query, nameof(Queryable.DefaultIfEmpty), [query.ElementType]);
        Assert.Null(Assert.Single(((IEnumerable)query).Cast<object?>()));
    }

    [Fact]
    public void Create_And_Constructor_Use_The_Same_Packing_For_Union_And_Join()
    {
        var source = new[] { 1, 2 }.AsQueryable().AsQuery();
        var created = source.Select(x => ValueTuple.Create(x, x + 1, x + 2, x + 3, x + 4, x + 5, x + 6, x + 7));
        var constructed = source.Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int>>(
            x, x + 1, x + 2, x + 3, x + 4, x + 5, x + 6, new ValueTuple<int>(x + 7)));
        Assert.Equal(((IShapedQuery)created).QueryShape.Source.ElementType, ((IShapedQuery)constructed).QueryShape.Source.ElementType);
        Assert.Equal(created.ToArray(), created.Union(constructed).OrderBy(x => x.Item1).ToArray());
        Assert.Equal([8, 9], created.Join(constructed, left => left.Rest.Item1, right => right.Rest.Item1,
            (left, right) => right.Rest.Item1).OrderBy(x => x).ToArray());
        Assert.Equal([9], created.Where(x => x.Rest.Item1 > 8).Select(x => x.Rest.Item1).ToArray());
    }

    [Fact]
    public async Task Sqlite_Union_And_Join_Use_Compatible_Carriers()
    {
        var source = _fixture.db.Accounts.Select(account => account.AccountId);
        var created = source.Select(x => ValueTuple.Create(x * 100, x * 100 + 1, x * 100 + 2, x * 100 + 3,
            x * 100 + 4, x * 100 + 5, x * 100 + 6, x * 100 + 7));
        var constructed = source.Select(x => new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int>>(
            x * 100, x * 100 + 1, x * 100 + 2, x * 100 + 3, x * 100 + 4, x * 100 + 5, x * 100 + 6,
            new ValueTuple<int>(x * 100 + 7)));
        var union = created.Union(constructed).OrderBy(row => row.Rest.Item1);
        AssertRows(union, 8, [1, 2, 3, 4, 5, 6, 7]);

        var joined = await union.Join(_fixture.db.Orders, row => row.Item1 / 100, order => order.AccountId,
            (row, order) => row).OrderBy(row => row.Rest.Item1).ToArrayAsync();
        var expected = await _fixture.db.Orders.Select(order => order.AccountId).OrderBy(id => id).ToArrayAsync();
        AssertRows(joined.AsQueryable(), 8, expected);
    }

    [Fact]
    public async Task Sqlite_Grouping_Can_Project_Eight_Items()
    {
        var expected = _fixture.db.Orders.AsEnumerable().GroupBy(order => order.CurrencyId)
            .Select(group => ValueTuple.Create(group.Key, group.Count(), group.Sum(order => order.Total),
                group.Min(order => order.Total), group.Max(order => order.Total), group.Count(order => order.Total > 50),
                group.Max(order => order.AccountId), group.Min(order => order.AccountId)))
            .OrderBy(row => row.Item1).ToArray();
        var actual = await _fixture.db.Orders.GroupBy(order => order.CurrencyId)
            .Select(group => ValueTuple.Create(group.Key, group.Count(), group.Sum(order => order.Total),
                group.Min(order => order.Total), group.Max(order => order.Total), group.Count(order => order.Total > 50),
                group.Max(order => order.AccountId), group.Min(order => order.AccountId)))
            .OrderBy(row => row.Item1).ToArrayAsync();

        Assert.NotEmpty(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Explicitly_Nested_Tuples_Stay_Individual_Items()
    {
        var source = new[] { 1 }.AsQueryable().AsQuery();
        var item = Expression.Parameter(typeof(int), "item");
        Expression[] values = [Tuple([item, Expression.Add(item, Expression.Constant(1))]),
            .. Enumerable.Range(1, 13).Select(index => (Expression)Expression.Add(item, Expression.Constant(index)))];
        var body = Tuple(values);
        var query = Apply(source, nameof(Queryable.Select), [typeof(int), body.Type], Expression.Quote(Expression.Lambda(body, item)));
        var row = Assert.Single(((IEnumerable)query).Cast<ITuple>());
        Assert.Equal(14, row.Length);
        var nested = Assert.IsAssignableFrom<ITuple>(row[0]);
        Assert.Equal(2, nested.Length);
        Assert.Equal(1, nested[0]);
        Assert.Equal(2, nested[1]);
        Assert.All(Enumerable.Range(1, 13), index => Assert.Equal(index + 1, row[index]));
    }

    private static IQueryable Project(IQueryable<int> source, int arity, bool nullable = false)
    {
        var item = Expression.Parameter(typeof(int), "item");
        Expression body = Tuple(Enumerable.Range(0, arity).Select(index => (Expression)Expression.Add(
            Expression.Multiply(item, Expression.Constant(100)), Expression.Constant(index))).ToArray());
        if (nullable)
            body = Expression.Convert(body, typeof(Nullable<>).MakeGenericType(body.Type));
        return Apply(source, nameof(Queryable.Select), [typeof(int), body.Type], Expression.Quote(Expression.Lambda(body, item)));
    }

    private static NewExpression Tuple(IReadOnlyList<Expression> items)
    {
        Expression[] arguments = items.Count > 7 ? [.. items.Take(7), Tuple(items.Skip(7).ToArray())] : items.ToArray();
        var types = arguments.Select(argument => argument.Type).ToArray();
        var type = TupleDefinitions[types.Length - 1].MakeGenericType(types);
        return Expression.New(type.GetConstructor(types)!, arguments);
    }

    private static Expression Item(Expression tuple, int index)
    {
        while (index >= 7)
        {
            tuple = Expression.Field(tuple, "Rest");
            index -= 7;
        }
        return Expression.Field(tuple, $"Item{index + 1}");
    }

    private static IQueryable Apply(IQueryable source, string name, Type[] types, params Expression[] arguments) =>
        source.Provider.CreateQuery(Expression.Call(typeof(Queryable), name, types, new[] { source.Expression }.Concat(arguments).ToArray()));

    private static void AssertRows(IQueryable query, int arity, int[] expected)
    {
        var rows = ((IEnumerable)query).Cast<ITuple>().ToArray();
        Assert.Equal(expected.Length, rows.Length);
        for (var row = 0; row < rows.Length; row++)
        {
            Assert.Equal(arity, rows[row].Length);
            for (var index = 0; index < arity; index++)
                Assert.Equal(expected[row] * 100 + index, rows[row][index]);
        }
    }

    private static void AssertCarrierArities(Type type)
    {
        if (!CarrierProjection.IsCarrier(type))
            return;
        Assert.InRange(type.GetGenericArguments().Length, 1, 7);
        Assert.All(type.GetGenericArguments(), AssertCarrierArities);
    }

    private static readonly Type[] TupleDefinitions =
    [
        typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
        typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>)
    ];
}
