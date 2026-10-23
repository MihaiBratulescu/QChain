using Microsoft.EntityFrameworkCore;
using QChain.Tests.TestBench;
using Samples.OnlineShop.DatabaseModels;
using Xunit.Abstractions;

namespace QChain.Tests;

public class SqliteTupleRewriting(SqliteFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    private IQueryable<int> Ids => _fixture.db.Accounts.Select(account => account.AccountId);
    private IQueryable<int> NativeIds => _fixture.db.Set<Account>().Select(account => account.AccountId);

    [Fact]
    public async Task Captured_Members_Work_In_Filter_And_Projection()
    {
        var captured = (Id: 2, Code: 12);
        var query = Ids.Where(id => id == captured.Id)
            .Select(id => ValueTuple.Create(id, captured.Code))
            .Distinct().Where(pair => pair.Item2 == 12);

        await AssertRows(query, [(2, 12)], "DISTINCT", "WHERE");
    }

    [Fact]
    public async Task Captured_Tuple_Changes_Are_Observed_Between_Executions()
    {
        var captured = (Id: 2, Code: 12);
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => pair.Item1 == captured.Id && pair.Item2 == captured.Code);

        await AssertRows(query, [(2, 12)], "WHERE");
        captured = (3, 13);
        await AssertRows(query, [(3, 13)], "WHERE");
    }

    [Theory]
    [MemberData(nameof(CapturedTuples.TupleArrays), MemberType = typeof(CapturedTuples))]
    public async Task Captured_Array_Contains_Translates((int, int)[] captured)
    {
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => captured.Contains(pair)).OrderBy(pair => pair.Item1);

        await AssertRows(query, captured, "WHERE", "ORDER BY");
    }

    [Theory]
    [MemberData(nameof(CapturedTuples.TupleArrays), MemberType = typeof(CapturedTuples))]
    public async Task Captured_Array_Enumerable_Contains_Translates((int, int)[] captured)
    {
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => Enumerable.Contains(captured, pair)).OrderBy(pair => pair.Item1);

        await AssertRows(query, captured, "WHERE", "ORDER BY");
    }

    [Theory]
    [MemberData(nameof(CapturedTuples.TupleLists), MemberType = typeof(CapturedTuples))]
    public async Task Captured_List_Contains_Translates(List<(int, int)> captured)
    {
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => captured.Contains(pair)).OrderBy(pair => pair.Item1);

        await AssertRows(query, captured.ToArray(), "WHERE", "ORDER BY");
    }

    [Theory]
    [MemberData(nameof(CapturedTuples.TupleArrays), MemberType = typeof(CapturedTuples))]
    public async Task Captured_Array_Any_Translates((int, int)[] captured)
    {
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => captured.Any(item => item.Item1 == pair.Item1 && item.Item2 == pair.Item2))
            .OrderBy(pair => pair.Item1);

        await AssertRows(query, captured, "WHERE", "ORDER BY");
    }

    [Theory]
    [MemberData(nameof(CapturedTuples.TupleLists), MemberType = typeof(CapturedTuples))]
    public async Task Captured_List_Any_Translates(List<(int, int)> captured)
    {
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => captured.Any(item => item.Item1 == pair.Item1 && item.Item2 == pair.Item2))
            .OrderBy(pair => pair.Item1);

        await AssertRows(query, captured.ToArray(), "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Captured_Array_Contains_Observes_Changes_Between_Executions()
    {
        var captured = new[] { (1, 11) };
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => captured.Contains(pair)).OrderBy(pair => pair.Item1);

        await AssertRows(query, [(1, 11)], "WHERE");
        captured[0] = (2, 12);
        await AssertRows(query, [(2, 12)], "WHERE");
        captured = [(3, 13), (5, 15)];
        await AssertRows(query, [(3, 13), (5, 15)], "WHERE");
        Assert.Equal(2, await query.CountAsync());
        captured = [];
        await AssertRows(query, [], "WHERE");
        Assert.Equal(0, await query.CountAsync());
    }

    [Fact]
    public async Task Captured_List_Any_Observes_Changes_Between_Executions()
    {
        var captured = new List<(int, int)> { (1, 11) };
        var query = Ids.Select(id => ValueTuple.Create(id, id + 10))
            .Where(pair => captured.Any(item => item.Item1 == pair.Item1 && item.Item2 == pair.Item2))
            .OrderBy(pair => pair.Item1);

        await AssertRows(query, [(1, 11)], "WHERE");
        captured.Clear();
        await AssertRows(query, [], "WHERE");
        captured.AddRange([(2, 12), (4, 14)]);
        await AssertRows(query, [(2, 12), (4, 14)], "WHERE");
        captured = [(3, 13)];
        await AssertRows(query, [(3, 13)], "WHERE");
        Assert.True(await query.AnyAsync());
    }

    [Fact]
    public async Task Captured_Contains_Matches_Whole_Pairs_With_Null_Components_And_Duplicates()
    {
        var captured = new (int, string?)[] { (1, "Gamma"), (3, "Alpha"), (5, "Epsilon"), (5, "Epsilon"), (6, null) };
        var query = _fixture.db.Accounts.Select(account => ValueTuple.Create(account.AccountId, account.Email))
            .Where(pair => captured.Contains(pair)).OrderBy(pair => pair.Item1);

        await AssertRows(query, [(5, "Epsilon"), (6, (string?)null)], "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Captured_Array_Index_Composes_In_SQL()
    {
        var captured = new[] { (1, 11), (3, 13) };
        var query = Ids.Select(id => ValueTuple.Create(id, captured[1].Item2))
            .Where(pair => pair.Item1 == captured[1].Item1).Distinct();

        await AssertRows(query, [(3, 13)], "DISTINCT", "WHERE");
    }

    [Fact]
    public async Task Captured_List_Index_Composes_In_SQL()
    {
        var captured = new List<(int, int)> { (1, 11), (3, 13) };
        var query = Ids.Select(id => ValueTuple.Create(id, captured[1].Item2))
            .Where(pair => pair.Item1 == captured[1].Item1).Distinct();

        await AssertRows(query, [(3, 13)], "DISTINCT", "WHERE");
    }

    [Fact]
    public async Task Nullable_Tuple_Materializes_Present_And_Absent_Values()
    {
        var query = Ids.OrderBy(id => id)
            .Select(id => id % 2 == 0 ? (ValueTuple<int, int>?)null : ValueTuple.Create(id, id + 10));

        await AssertRows(query, [(1, 11), null, (3, 13), null, (5, 15), null, (7, 17)], "ORDER BY");
    }

    [Fact]
    public async Task Nullable_Tuple_HasValue_Translates()
    {
        var native = NativeIds.Select(id => id % 2 == 0 ? (int?)null : id)
            .Select(value => value.HasValue).Distinct().OrderBy(present => present);
        await AssertRows(native, [false, true], "DISTINCT", "ORDER BY");

        var query = Ids.Select(id => id % 2 == 0 ? (ValueTuple<int, int>?)null : ValueTuple.Create(id, id + 10))
            .Select(pair => pair.HasValue).Distinct().OrderBy(present => present);

        await AssertRows(query, [false, true], "DISTINCT", "ORDER BY");
    }

    [Fact]
    public async Task Nullable_Tuple_Guarded_Value_Access_Translates()
    {
        var query = Ids.Select(id => id % 2 == 0 ? (ValueTuple<int, int>?)null : ValueTuple.Create(id, id + 10))
            .Where(pair => pair.HasValue && pair.Value.Item1 > 2)
            .OrderBy(pair => pair!.Value.Item1).Select(pair => pair!.Value);

        await AssertRows(query, [(3, 13), (5, 15), (7, 17)], "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Nullable_Tuple_Null_Comparison_Translates()
    {
        var query = Ids.Select(id => new
            {
                Id = id,
                Pair = id % 2 == 0 ? (ValueTuple<int, int>?)null : ValueTuple.Create(id, id + 10)
            })
            .Where(row => row.Pair == null).OrderBy(row => row.Id).Select(row => row.Id);

        await AssertRows(query, [2, 4, 6], "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Nullable_Tuple_Coalesce_Composes_After_Distinct()
    {
        var query = Ids.Select(id => id % 2 == 0 ? (ValueTuple<int, int>?)null : ValueTuple.Create(id, id + 10))
            .Select(pair => pair ?? ValueTuple.Create(0, 10))
            .Distinct().Where(pair => pair.Item2 >= 10).OrderBy(pair => pair.Item1);

        await AssertRows(query, [(0, 10), (1, 11), (3, 13), (5, 15), (7, 17)], "DISTINCT", "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Nullable_Tuple_GetValueOrDefault_Translates()
    {
        var query = Ids.Select(id => id % 2 == 0 ? (ValueTuple<int, int>?)null : ValueTuple.Create(id, id + 10))
            .Select(pair => pair.GetValueOrDefault())
            .Distinct().Where(pair => pair.Item1 == 0 || pair.Item2 > 12).OrderBy(pair => pair.Item1);

        await AssertRows(query, [(0, 0), (3, 13), (5, 15), (7, 17)], "DISTINCT", "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Nested_Tuple_In_Anonymous_Projection_Composes_After_Distinct()
    {
        var query = Ids.Select(id => new { Value = ValueTuple.Create(id, ValueTuple.Create(id + 10)), Label = id })
            .Distinct().Where(row => row.Value.Item2.Item1 > 14).OrderBy(row => row.Label)
            .Select(row => ValueTuple.Create(row.Label, row.Value.Item2.Item1));

        await AssertRows(query, [(5, 15), (6, 16), (7, 17)], "DISTINCT", "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Tuple_Member_Initializer_Composes_After_Distinct()
    {
        var query = Ids.Select(id => new ValueTuple<int, int> { Item1 = id % 3, Item2 = id % 3 + 10 })
            .Distinct().Where(pair => pair.Item1 > 0).OrderBy(pair => pair.Item2);

        await AssertRows(query, [(1, 11), (2, 12)], "DISTINCT", "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Partial_Tuple_Initializer_Preserves_Defaults_In_SQL()
    {
        var query = Ids.Select(id => new ValueTuple<int, int> { Item2 = id + 10 })
            .Distinct().Where(pair => pair.Item1 == 0 && pair.Item2 > 14).OrderBy(pair => pair.Item2);

        await AssertRows(query, [(0, 15), (0, 16), (0, 17)], "DISTINCT", "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Tuple_Initializer_Overrides_Constructor_In_SQL()
    {
        var query = Ids.Select(id => new ValueTuple<int, int>(id, 99) { Item2 = id + 10 })
            .Where(pair => pair.Item2 < 13).OrderBy(pair => pair.Item1);

        await AssertRows(query, [(1, 11), (2, 12)], "WHERE", "ORDER BY");
    }

    [Fact]
    public async Task Nested_Tuple_Initializer_Composes_After_Distinct()
    {
        var query = Ids.Select(id => new ValueTuple<int, ValueTuple<int, int>>
            {
                Item1 = id,
                Item2 = new ValueTuple<int, int> { Item1 = id + 10, Item2 = id + 20 }
            })
            .Distinct().Where(pair => pair.Item2.Item1 > 14).OrderBy(pair => pair.Item1)
            .Select(pair => ValueTuple.Create(pair.Item1, pair.Item2.Item2));

        await AssertRows(query, [(5, 25), (6, 26), (7, 27)], "DISTINCT", "WHERE", "ORDER BY");
    }

    private static async Task AssertRows<T>(IQueryable<T> query, T[] expected, params string[] sqlClauses)
    {
        var sql = query.ToQueryString();
        foreach (var clause in sqlClauses)
            Assert.Contains(clause, sql);
        Assert.Equal(expected, await query.ToArrayAsync());
    }
}
