using QChain.Visitors;

using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    #region Internal Query
    protected IQueryable<Q> Source { get; }
    protected Expression<Func<Q, T>> Shape { get; }
    LambdaExpression IInternalQuery.UntypedShape => Shape;
    #endregion

    #region Constructors
    internal DeferredQuery(IQueryable<Q> source, Expression<Func<Q, T>> shape) =>
        (Source, Shape) = (source, shape);

    protected DeferredQuery(DeferredQuery<T, Q> query) =>
        (Source, Shape) = (query.Source, query.Shape);
    #endregion

    public IQueryable<T> AsQueryable() => Source.Select(Shape);

    #region Helpers
    private Expression<Func<Q, TResult>> Translate<TResult>(
        Expression<Func<T, TResult>> expression)
    {
        var body = new ProjectionInliningVisitor(expression.Parameters[0], Shape.Body).Visit(expression.Body)!;

        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<Q, TResult>>(body, Shape.Parameters);
    }

    private static Expression<Func<TSource, TResult>> Compose<TSource, TMiddle, TResult>(
        Expression<Func<TMiddle, TResult>> outer, Expression<Func<TSource, TMiddle>> inner)
    {
        var body = ReplaceExpressionVisitor.Replace(outer.Body, outer.Parameters[0], inner.Body);

        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<TSource, TResult>>(body, inner.Parameters);
    }
    #endregion

    private readonly struct Pair<T1, T2>
    {
        public required T1 Left { get; init; }
        public required T2 Right { get; init; }
    }
}
