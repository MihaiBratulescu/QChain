using QChain.Internal.Helpers;
using QChain.Internal.Visitors;
using System.Linq.Expressions;

namespace QChain.Internal;

internal abstract class QueryShape<T> : IQueryShape
{
    public abstract IQueryable UntypedSource { get; }
    public abstract LambdaExpression UntypedShape { get; }
    public abstract Type SourceType { get; }

    public abstract IQueryable<T> Project();
}

internal abstract class QueryShape<T, Q> : QueryShape<T>
{
    protected QueryShape(IQueryable<Q> source, Expression<Func<Q, T>> shape)
    {
        Source = source;
        Shape = shape;
    }

    public IQueryable<Q> Source { get; }
    public Expression<Func<Q, T>> Shape { get; }

    public override IQueryable UntypedSource => Source;
    public override LambdaExpression UntypedShape => Shape;
    public override Type SourceType => typeof(Q);

    public override IQueryable<T> Project() => Source.Select(Shape);

    public virtual Expression<Func<Q, TResult>> Translate<TResult>(Expression<Func<T, TResult>> expression) =>
        ComposeInternal(expression);

    protected virtual Expression<Func<Q, R>> ComposeInternal<R>(Expression<Func<T, R>> outer)
    {
        var body = ReplaceExpressionVisitor.Replace(outer.Body, outer.Parameters[0], Shape.Body);

        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Q, R>>(body, Shape.Parameters);
    }

    protected internal Expression<Func<TCarrier, T>> Rebuild<TCarrier>()
    {
        var carrier = Expression.Parameter(typeof(TCarrier), "x");

        return Expression.Lambda<Func<TCarrier, T>>(
            TupleProjection<T, TCarrier>.Rebuild(carrier, typeof(T)),
            carrier);
    }
}
