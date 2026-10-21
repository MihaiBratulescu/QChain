using QChain.Internal.Helpers;
using System.Linq.Expressions;

namespace QChain.Internal.Types;

internal interface IQueryShape
{
    IQueryable Source { get; }
    LambdaExpression Shape { get; }
    Type SourceType { get; }
    IQueryable Project();
}

internal sealed class QueryShape<T>(IQueryable<T> source) : IQueryShape
{
    public IQueryable Source => source;
    public LambdaExpression Shape { get; } = (Expression<Func<T, T>>)(item => item);
    public Type SourceType => typeof(T);
    public IQueryable Project() => source;
}

internal sealed class QueryShape<T, Q>(IQueryable<Q> source) : IQueryShape
{
    public IQueryable Source => source;
    public LambdaExpression Shape { get; } = CreateProjection();
    public Type SourceType => typeof(Q);

    public IQueryable Project() => source.Select((Expression<Func<Q, T>>)Shape);

    private static Expression<Func<Q, T>> CreateProjection()
    {
        var item = Expression.Parameter(typeof(Q), "item");
        return Expression.Lambda<Func<Q, T>>(CarrierProjection.Rebuild(item, typeof(T)), item);
    }
}
