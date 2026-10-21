using System.Linq.Expressions;
using Reduction = QChain.Internal.Helpers.ProjectionReduction;

namespace QChain.Tests;

public class ProjectionReduction
{
    [Fact]
    public void Anonymous_Member_Resolves_To_Its_Constructor_Argument()
    {
        Expression<Func<int, int>> expression = item => new { First = item, Last = item + 1 }.Last;
        var access = Assert.IsAssignableFrom<MemberExpression>(expression.Body);
        var projection = Assert.IsAssignableFrom<NewExpression>(access.Expression);

        Assert.True(Reduction.TryInlineMemberAccess(projection, access.Member, out var value));

        Assert.Same(projection.Arguments[1], value);
        Assert.Equal(expression.Compile()(5), Expression.Lambda<Func<int, int>>(value, expression.Parameters).Compile()(5));
    }

    [Fact]
    public void Constructor_Member_Not_In_Projection_Is_Not_Inlined()
    {
        var type = typeof(ProjectionValue);
        var projection = Expression.New(type.GetConstructor([typeof(int)])!,
            [Expression.Constant(5)], [type.GetProperty(nameof(ProjectionValue.Assigned))!]);

        Assert.False(Reduction.TryInlineMemberAccess(projection,
            type.GetProperty(nameof(ProjectionValue.Unassigned))!, out var value));

        Assert.Null(value);
    }

    [Fact]
    public void Constructor_Without_Member_Metadata_Is_Not_Inlined()
    {
        var type = typeof(ProjectionValue);
        var projection = Expression.New(type.GetConstructor([typeof(int)])!, Expression.Constant(5));

        Assert.False(Reduction.TryInlineMemberAccess(projection,
            type.GetProperty(nameof(ProjectionValue.Assigned))!, out var value));

        Assert.Null(value);
    }

    [Fact]
    public void Initialized_Member_Resolves_To_Its_Assigned_Expression()
    {
        Expression<Func<int, int>> expression = item => new ProjectionValue { Assigned = item + 1 }.Assigned;
        var access = Assert.IsAssignableFrom<MemberExpression>(expression.Body);
        var projection = Assert.IsAssignableFrom<MemberInitExpression>(access.Expression);
        var assignment = Assert.IsType<MemberAssignment>(Assert.Single(projection.Bindings));

        Assert.True(Reduction.TryInlineMemberAccess(projection, access.Member, out var value));

        Assert.Same(assignment.Expression, value);
        Assert.Equal(expression.Compile()(5), Expression.Lambda<Func<int, int>>(value, expression.Parameters).Compile()(5));
    }

    [Fact]
    public void Unassigned_Member_Is_Not_Inlined_To_A_Default_Value()
    {
        Expression<Func<int, int>> expression = item => new ProjectionValue { Assigned = item }.Unassigned;
        var access = Assert.IsAssignableFrom<MemberExpression>(expression.Body);

        Assert.False(Reduction.TryInlineMemberAccess(access.Expression!, access.Member, out var value));

        Assert.Null(value);
        Assert.Equal(42, expression.Compile()(5));
    }

    [Fact]
    public void Parameter_Member_Is_Not_Inlined()
    {
        var parameter = Expression.Parameter(typeof(ProjectionValue), "item");
        var member = typeof(ProjectionValue).GetProperty(nameof(ProjectionValue.Assigned))!;

        Assert.False(Reduction.TryInlineMemberAccess(parameter, member, out var value));

        Assert.Null(value);
    }

    private sealed class ProjectionValue
    {
        public ProjectionValue() { }
        public ProjectionValue(int assigned) => Assigned = assigned;

        public int Assigned { get; init; }
        public int Unassigned { get; init; } = 42;
    }
}
