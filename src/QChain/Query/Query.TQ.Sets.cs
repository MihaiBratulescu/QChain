using QChain.Internal;
using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IUntypedQuery
{
    public IQuery<T> Union(IQuery<T> other) =>
        SetOperation(other, SetOperationKind.Union);

    public IQuery<T> Concat(IQuery<T> other) =>
        SetOperation(other, SetOperationKind.Concat);

    public IQuery<T> Except(IQuery<T> other) =>
        SetOperation(other, SetOperationKind.Except);

    public IQuery<T> ExceptBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector)
    {
        var translated = QueryShape.Translate(keySelector);
        var parameter = translated.Parameters[0];

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(K)],
            Expression.Constant(keys),
            translated.Body);

        var predicate = Expression.Lambda<Func<Q, bool>>(
            Expression.Not(contains), parameter);

        return new Query<T, Q>(QueryShape.Source.Where(predicate), QueryShape.Shape);
    }

    public IQuery<T> Intersect(IQuery<T> other) =>
        SetOperation(other, SetOperationKind.Intersect);

    public IQuery<T> IntersectBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector)
    {
        var translated = QueryShape.Translate(keySelector);

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(K)],
            Expression.Constant(keys),
            translated.Body);

        var predicate = Expression.Lambda<Func<Q, bool>>(
            contains, translated.Parameters);

        return new Query<T, Q>(QueryShape.Source.Where(predicate), QueryShape.Shape);
    }

    #region Helpers
    private IQuery<T> SetOperation(IQuery<T> other, SetOperationKind kind)
    {
        var right = (other as IUntypedQuery)!;
        var carrier = TupleProjection<T, Q>.Lower(QueryShape.Shape.Body).Type;
        var qr = right.Untyped.UntypedShape.Parameters[0].Type;

        return (IQuery<T>)SetOperationTypedMethod
            .MakeGenericMethod(qr, carrier)
            .Invoke(this, [right, kind])!;
    }

    private Query<T, C> SetOperationTyped<QR, C>(IUntypedQuery rightUntyped, SetOperationKind kind)
    {
        var rightSource = (IQueryable<QR>)rightUntyped.Untyped.UntypedSource;
        var rightShape = (Expression<Func<QR, T>>)rightUntyped.Untyped.UntypedShape;

        var left = QueryShape.Source.Select(BuildCarrierShape<Q, C>(QueryShape.Shape));
        var right = rightSource.Select(BuildCarrierShape<QR, C>(rightShape));

        var source = kind switch
        {
            SetOperationKind.Union => left.Union(right),
            SetOperationKind.Concat => left.Concat(right),
            SetOperationKind.Except => left.Except(right),
            SetOperationKind.Intersect => left.Intersect(right),
            _ => throw new NotSupportedException(kind.ToString())
        };

        var carrier = Expression.Parameter(typeof(C), "x");
        var shape = Expression.Lambda<Func<C, T>>(
            TupleProjection<T, C>.Rebuild(carrier, typeof(T)),
            carrier);

        return new Query<T, C>(source, shape);
    }

    private static Expression<Func<TSource, C>> BuildCarrierShape<TSource, C>(Expression<Func<TSource, T>> shape)
    {
        var body = TupleProjection<T, TSource>.Lower(shape.Body);
        return Expression.Lambda<Func<TSource, C>>(body, shape.Parameters);
    }

    private static readonly MethodInfo SetOperationTypedMethod =
        typeof(Query<T, Q>).GetMethod(nameof(SetOperationTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
    #endregion
}

internal enum SetOperationKind
{
    Union,
    Concat,
    Except,
    Intersect
}
