using System.Linq.Expressions;

namespace QChain;

internal interface IQueryShape
{
    IQueryable Source { get; }
    LambdaExpression Shape { get; }
}

internal sealed class QueryShape<T, Q>(IQueryable<Q> source, Expression<Func<Q, T>> shape) : IQueryShape
{
    public IQueryable<Q> Source { get; } = source;
    public Expression<Func<Q, T>> Shape { get; } = shape;

    IQueryable IQueryShape.Source => Source;
    LambdaExpression IQueryShape.Shape => Shape;
}
