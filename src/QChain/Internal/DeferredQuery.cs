using QChain.Visitors;
using System.Collections;
using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    #region Internal Query
    protected IQueryable<Q> Source { get; }
    protected Expression<Func<Q, T>> Shape { get; }
    IQueryable IInternalQuery.UntypedSource => Source;
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
        Expression<Func<T, TResult>> expression) =>
        Compose(expression, Shape);

    private static Expression<Func<TSource, TResult>> Compose<TSource, TMiddle, TResult>(
        Expression<Func<TMiddle, TResult>> outer, Expression<Func<TSource, TMiddle>> inner)
    {
        var body = ReplaceExpressionVisitor.Replace(outer.Body, outer.Parameters[0], inner.Body);

        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<TSource, TResult>>(body, inner.Parameters);
    }

    #endregion
}

internal readonly struct Pair<T1, T2>
{
    public required T1 Left { get; init; }
    public required T2 Right { get; init; }
}

internal readonly struct Projection<T1, T2>
{
    public required T1 Item1 { get; init; }
    public required T2 Item2 { get; init; }
}

internal sealed class ShapedGroupingValue<KInternal, K, EInternal, E> : IGrouping<K, E>
{
    public required KInternal InternalKey { get; init; }
    public required IEnumerable<EInternal> InternalItems { get; init; }
    public required Func<KInternal, K> KeyShape { get; init; }
    public required Func<EInternal, E> ElementShape { get; init; }

    public K Key => KeyShape(InternalKey);

    public IEnumerator<E> GetEnumerator() => InternalItems.Select(ElementShape).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class GroupingShapeHolder<KInternal, K, EInternal, E>
{
    public required Func<KInternal, K> KeyShape { get; init; }
    public required Func<EInternal, E> ElementShape { get; init; }
}
