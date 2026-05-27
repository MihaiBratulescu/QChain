using QChain.Internal.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal static class SetShapeBuilder<T, Q>
{
    public static IQueryShape Distinct(SequenceQueryShape<T, Q> shape)
    {
        var lowered = TupleProjection<T, Q>.Lower(shape.Shape.Body);

        return (IQueryShape)DistinctTypedMethod
            .MakeGenericMethod(lowered.Type)
            .Invoke(null, [shape, lowered])!;
    }

    public static IQueryShape Union(SequenceQueryShape<T, Q> left, IQueryShape right) =>
        SetOperation(left, right, SetOperationKind.Union);

    public static IQueryShape Concat(SequenceQueryShape<T, Q> left, IQueryShape right) =>
        SetOperation(left, right, SetOperationKind.Concat);

    public static IQueryShape Except(SequenceQueryShape<T, Q> left, IQueryShape right) =>
        SetOperation(left, right, SetOperationKind.Except);

    public static IQueryShape Intersect(SequenceQueryShape<T, Q> left, IQueryShape right) =>
        SetOperation(left, right, SetOperationKind.Intersect);

    private static SequenceQueryShape<T, TCarrier> DistinctTyped<TCarrier>(
        SequenceQueryShape<T, Q> shape,
        Expression lowered)
    {
        var carrierShape = Expression.Lambda<Func<Q, TCarrier>>(lowered, shape.Shape.Parameters);

        return new SetQueryShape<T, TCarrier>(
            shape.Source.Select(carrierShape).Distinct(),
            shape.Rebuild<TCarrier>());
    }

    private static IQueryShape SetOperation(SequenceQueryShape<T, Q> left, IQueryShape right, SetOperationKind kind)
    {
        var carrier = TupleProjection<T, Q>.Lower(left.Shape.Body).Type;

        return (IQueryShape)SetOperationTypedMethod
            .MakeGenericMethod(right.SourceType, carrier)
            .Invoke(null, [left, right, kind])!;
    }

    private static SequenceQueryShape<T, C> SetOperationTyped<QR, C>(
        SequenceQueryShape<T, Q> left,
        IQueryShape rightUntyped,
        SetOperationKind kind)
    {
        var right = (QueryShape<T, QR>)rightUntyped;

        var leftCarrier = left.Source.Select(BuildCarrierShape<Q, C>(left.Shape));
        var rightCarrier = right.Source.Select(BuildCarrierShape<QR, C>(right.Shape));

        var source = kind switch
        {
            SetOperationKind.Union => leftCarrier.Union(rightCarrier),
            SetOperationKind.Concat => leftCarrier.Concat(rightCarrier),
            SetOperationKind.Except => leftCarrier.Except(rightCarrier),
            SetOperationKind.Intersect => leftCarrier.Intersect(rightCarrier),
            _ => throw new NotSupportedException(kind.ToString())
        };

        return new SetQueryShape<T, C>(source, left.Rebuild<C>());
    }

    private static Expression<Func<TSource, C>> BuildCarrierShape<TSource, C>(Expression<Func<TSource, T>> shape)
    {
        var body = TupleProjection<T, TSource>.Lower(shape.Body);
        return Expression.Lambda<Func<TSource, C>>(body, shape.Parameters);
    }

    private static readonly MethodInfo DistinctTypedMethod =
        typeof(SetShapeBuilder<T, Q>).GetMethod(nameof(DistinctTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo SetOperationTypedMethod =
        typeof(SetShapeBuilder<T, Q>).GetMethod(nameof(SetOperationTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private enum SetOperationKind
    {
        Union,
        Concat,
        Except,
        Intersect
    }
}
