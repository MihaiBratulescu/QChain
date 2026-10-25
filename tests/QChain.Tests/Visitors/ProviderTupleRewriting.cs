using QChain.Internal.Helpers;
using QChain.Internal.Visitors;
using System.Linq.Expressions;

namespace QChain.Tests.Visitors;

public class ProviderTupleRewriting
{
    [Theory]
    [InlineData(true, 11)]
    [InlineData(false, 22)]
    public void Constant_Condition_Folds_To_Selected_Scalar(bool condition, int expected)
    {
        var expression = Expression.Condition(Expression.Constant(condition),
            Expression.Constant(11), Expression.Constant(22));

        var rewritten = new ProviderTupleVisitor().Visit(expression);

        var constant = Assert.IsType<ConstantExpression>(rewritten);
        Assert.Equal(expected, constant.Value);
        Assert.Equal(expected, Expression.Lambda<Func<int>>(rewritten).Compile()());
    }

    [Fact]
    public void Constant_True_Reuses_True_Carrier_Branch()
    {
        var selected = CarrierProjection.Create([Expression.Constant(3), Expression.Constant(13)]);
        var expression = Expression.Condition(Expression.Constant(true), selected, Expression.Default(selected.Type));

        var rewritten = new ProviderTupleVisitor().Visit(expression);

        Assert.Same(selected, rewritten);
        var projection = CarrierProjection.Rebuild(rewritten, typeof(ValueTuple<int, int>));
        Assert.Equal((3, 13), Expression.Lambda<Func<ValueTuple<int, int>>>(projection).Compile()());
    }

    [Fact]
    public void Constant_False_Reuses_False_Carrier_Branch()
    {
        var selected = CarrierProjection.Create([Expression.Constant(5), Expression.Constant(15)]);
        var expression = Expression.Condition(Expression.Constant(false), Expression.Default(selected.Type), selected);

        var rewritten = new ProviderTupleVisitor().Visit(expression);

        Assert.Same(selected, rewritten);
        var projection = CarrierProjection.Rebuild(rewritten, typeof(ValueTuple<int, int>));
        Assert.Equal((5, 15), Expression.Lambda<Func<ValueTuple<int, int>>>(projection).Compile()());
    }
}
