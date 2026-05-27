using QChain.Internal.Builders;
using QChain.Internal.Helpers;
using QChain.Internal.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal sealed class ProjectedQueryShape<T, Q>(IQueryable<Q> source, Expression<Func<Q, T>> shape) 
    : SequenceQueryShape<T, Q>(source, shape)
{
    public static IQueryShape Compose<R>(SequenceQueryShape<T, Q> source, Expression<Func<T, R>> outer) =>
        new ProjectedQueryShape<R, Q>(source.Source, source.Translate(outer));

    public static IQueryShape SelectMany<R>(
        SequenceQueryShape<T, Q> source,
        Expression<Func<T, IEnumerable<R>>> collectionSelector)
    {
        var translated = source.Translate(collectionSelector);

        if (translated.Body is not MethodCallExpression call)
        {
            return new ProjectedQueryShape<R, R>(
                source.Source.SelectMany(translated),
                item => item);
        }

        var collectionSource = call.Arguments[0];
        var itemShape = (LambdaExpression)call.Arguments[1];
        var elementType = itemShape.Parameters[0].Type;
        var collectionSelectorTyped = BuildCollectionSelector(translated, elementType, collectionSource);

        return (IQueryShape)SelectManyTypedMethod
            .MakeGenericMethod(typeof(R), elementType)
            .Invoke(null, [source, collectionSelectorTyped, itemShape])!;
    }

    public static SequenceQueryShape<R, Pair<Q, C>> SelectMany<C, R>(
        SequenceQueryShape<T, Q> source,
        Expression<Func<T, IEnumerable<C>>> collectionSelector,
        Expression<Func<T, C, R>> resultSelector)
    {
        var projectedSource = source.Source.SelectMany(
            source.Translate(collectionSelector),
            (q, c) => new Pair<Q, C>
            {
                Left = q,
                Right = c
            });

        return new ProjectedQueryShape<R, Pair<Q, C>>(
            projectedSource,
            TranslateSelectManyResult(source.Shape, resultSelector));
    }

    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new ProjectedQueryShape<T, Q>(source, Shape);

    private static LambdaExpression BuildCollectionSelector(LambdaExpression selector, Type elementType, Expression body)
    {
        return Expression.Lambda(
            typeof(Func<,>).MakeGenericType(selector.Parameters[0].Type, typeof(IEnumerable<>).MakeGenericType(elementType)),
            body,
            selector.Parameters);
    }

    private static SequenceQueryShape<R, QR> SelectManyTyped<R, QR>(
        SequenceQueryShape<T, Q> source,
        LambdaExpression collectionSelectorUntyped,
        LambdaExpression itemShapeUntyped)
    {
        var collectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)collectionSelectorUntyped;
        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new ProjectedQueryShape<R, QR>(
            source.Source.SelectMany(collectionSelector),
            itemShape);
    }

    private static Expression<Func<Pair<Q, C>, R>> TranslateSelectManyResult<C, R>(
        Expression<Func<Q, T>> sourceShape,
        Expression<Func<T, C, R>> resultSelector)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, C>), "p");

        var outerQ = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Left));
        var innerC = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Right));

        var publicShape = ReplaceExpressionVisitor.Replace(sourceShape.Body, sourceShape.Parameters[0], outerQ);

        var body = ReplaceExpressionVisitor.ReplaceMany(resultSelector.Body, new Dictionary<Expression, Expression>
        {
            [resultSelector.Parameters[0]] = publicShape,
            [resultSelector.Parameters[1]] = innerC
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Pair<Q, C>, R>>(body, pair);
    }

    private static readonly MethodInfo SelectManyTypedMethod =
        typeof(ProjectedQueryShape<T, Q>).GetMethod(nameof(SelectManyTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
}

internal sealed class JoinedQueryShape<T, Q>(IQueryable<Q> source, Expression<Func<Q, T>> shape) 
    : SequenceQueryShape<T, Q>(source, shape)
{
    public static IQueryShape Join<R, K, TOut>(
        SequenceQueryShape<T, Q> left,
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R, TOut>> result)
    {
        return (IQueryShape)JoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(null, [left, right, leftKey, rightKey, result])!;
    }

    public static IQueryShape GroupJoin<R, K, TOut>(
        SequenceQueryShape<T, Q> left,
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        return (IQueryShape)GroupJoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(null, [left, right, leftKey, rightKey, result])!;
    }

    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new JoinedQueryShape<T, Q>(source, Shape);

    private static SequenceQueryShape<TOut, Pair<Q, QR>> JoinTyped<R, K, TOut, QR>(
        SequenceQueryShape<T, Q> left,
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var joined = left.Source.Join(
            right.Source,
            left.Translate(leftKey),
            right.Translate(rightKey),
            (leftRow, rightRow) => new Pair<Q, QR>
            {
                Left = leftRow,
                Right = rightRow
            });

        return new JoinedQueryShape<TOut, Pair<Q, QR>>(
            joined,
            BuildJoinShape(left.Shape, right.Shape, result));
    }

    private static Expression<Func<Pair<Q, QR>, TOut>> BuildJoinShape<R, QR, TOut>(
        Expression<Func<Q, T>> leftShape,
        Expression<Func<QR, R>> rightShape,
        Expression<Func<T, R, TOut>> result)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, QR>), "p");

        var leftInternal = Expression.Property(pair, nameof(Pair<Q, QR>.Left));
        var rightInternal = Expression.Property(pair, nameof(Pair<Q, QR>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(leftShape.Body, leftShape.Parameters[0], leftInternal);
        var rightPublic = ReplaceExpressionVisitor.Replace(rightShape.Body, rightShape.Parameters[0], rightInternal);

        var body = ReplaceExpressionVisitor.ReplaceMany(result.Body, new Dictionary<Expression, Expression>
        {
            [result.Parameters[0]] = leftPublic,
            [result.Parameters[1]] = rightPublic
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Pair<Q, QR>, TOut>>(body, pair);
    }

    private static SequenceQueryShape<TOut, Pair<Q, IEnumerable<QR>>> GroupJoinTyped<R, K, TOut, QR>(
        SequenceQueryShape<T, Q> left,
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var grouped = left.Source.GroupJoin(
            right.Source,
            left.Translate(leftKey),
            right.Translate(rightKey),
            (leftRow, rightRows) => new Pair<Q, IEnumerable<QR>>
            {
                Left = leftRow,
                Right = rightRows
            });

        return new JoinedQueryShape<TOut, Pair<Q, IEnumerable<QR>>>(
            grouped,
            BuildGroupJoinShape(left.Shape, right.Shape, result));
    }

    private static Expression<Func<Pair<Q, IEnumerable<QR>>, TOut>> BuildGroupJoinShape<R, QR, TOut>(
        Expression<Func<Q, T>> leftShape,
        Expression<Func<QR, R>> rightShape,
        Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, IEnumerable<QR>>), "p");

        var leftInternal = Expression.Property(pair, nameof(Pair<Q, IEnumerable<QR>>.Left));
        var rightInternal = Expression.Property(pair, nameof(Pair<Q, IEnumerable<QR>>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(leftShape.Body, leftShape.Parameters[0], leftInternal);
        var projectedRight = ComposeEnumerable(rightShape, rightInternal);

        var body = ReplaceExpressionVisitor.ReplaceMany(result.Body, new Dictionary<Expression, Expression>
        {
            [result.Parameters[0]] = leftPublic,
            [result.Parameters[1]] = projectedRight
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Pair<Q, IEnumerable<QR>>, TOut>>(body, pair);
    }

    private static MethodCallExpression ComposeEnumerable<TRightInternal, TRightPublic>(
        Expression<Func<TRightInternal, TRightPublic>> itemShape,
        Expression enumerableExpression)
    {
        return Expression.Call(
            EnumerableSelectMethod.MakeGenericMethod(typeof(TRightInternal), typeof(TRightPublic)),
            enumerableExpression,
            itemShape);
    }

#if NET10_0_OR_GREATER
    public static IQueryShape LeftJoin<R, K, TOut>(
        SequenceQueryShape<T, Q> left,
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R?, TOut>> result)
    {
        return (IQueryShape)LeftJoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(null, [left, right, leftKey, rightKey, result])!;
    }

    public static IQueryShape RightJoin<R, K, TOut>(
        SequenceQueryShape<T, Q> left,
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T?, R, TOut>> result)
    {
        return (IQueryShape)RightJoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(null, [left, right, leftKey, rightKey, result])!;
    }

    private static SequenceQueryShape<TOut, Pair<Q, QR?>> LeftJoinTyped<R, K, TOut, QR>(
        SequenceQueryShape<T, Q> left,
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R?, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var joined = left.Source.LeftJoin(
            right.Source,
            left.Translate(leftKey),
            right.Translate(rightKey),
            (leftRow, rightRow) => new Pair<Q, QR?>
            {
                Left = leftRow,
                Right = rightRow
            });

        return new JoinedQueryShape<TOut, Pair<Q, QR?>>(
            joined,
            BuildOuterJoinShape<Q, QR?, T, R?, TOut>(left.Shape, right.Shape, result));
    }

    private static SequenceQueryShape<TOut, Pair<Q?, QR>> RightJoinTyped<R, K, TOut, QR>(
        SequenceQueryShape<T, Q> left,
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T?, R, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var joined = left.Source.RightJoin(
            right.Source,
            left.Translate(leftKey),
            right.Translate(rightKey),
            (leftRow, rightRow) => new Pair<Q?, QR>
            {
                Left = leftRow,
                Right = rightRow
            });

        return new JoinedQueryShape<TOut, Pair<Q?, QR>>(
            joined,
            BuildOuterJoinShape<Q?, QR, T?, R, TOut>(left.Shape, right.Shape, result));
    }

    private static Expression<Func<Pair<TLeftSource, TRightSource>, TOut>> BuildOuterJoinShape<TLeftSource, TRightSource, TLeft, TRight, TOut>(
        LambdaExpression leftShape,
        LambdaExpression rightShape,
        Expression<Func<TLeft, TRight, TOut>> selector)
    {
        var pair = Expression.Parameter(typeof(Pair<TLeftSource, TRightSource>), "p");

        var leftQ = Expression.PropertyOrField(pair, nameof(Pair<TLeftSource, TRightSource>.Left));
        var rightQ = Expression.PropertyOrField(pair, nameof(Pair<TLeftSource, TRightSource>.Right));

        var left = ReplaceExpressionVisitor.Replace(leftShape.Body, leftShape.Parameters[0], leftQ);
        var right = ReplaceExpressionVisitor.Replace(rightShape.Body, rightShape.Parameters[0], rightQ);

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = left,
            [selector.Parameters[1]] = right
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Pair<TLeftSource, TRightSource>, TOut>>(body, pair);
    }

    private static readonly MethodInfo LeftJoinTypedMethod =
        typeof(JoinedQueryShape<T, Q>).GetMethod(nameof(LeftJoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo RightJoinTypedMethod =
        typeof(JoinedQueryShape<T, Q>).GetMethod(nameof(RightJoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
#endif

    private static readonly MethodInfo JoinTypedMethod =
        typeof(JoinedQueryShape<T, Q>).GetMethod(nameof(JoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo GroupJoinTypedMethod =
        typeof(JoinedQueryShape<T, Q>).GetMethod(nameof(GroupJoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));
}

internal sealed class SetQueryShape<T, Q>(IQueryable<Q> source, Expression<Func<Q, T>> shape) 
    : SequenceQueryShape<T, Q>(source, shape)
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

    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new SetQueryShape<T, Q>(source, Shape);

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
        typeof(SetQueryShape<T, Q>).GetMethod(nameof(DistinctTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo SetOperationTypedMethod =
        typeof(SetQueryShape<T, Q>).GetMethod(nameof(SetOperationTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private enum SetOperationKind
    {
        Union,
        Concat,
        Except,
        Intersect
    }
}

internal sealed class GroupedQueryShape<K, KQ, E, QG, T, Q>
    : SequenceQueryShape<IGrouping<K, E>, IGrouping<KQ, QG>>
{
    private readonly Expression<Func<QG, E>> _elementShape;

    internal GroupedQueryShape(
        IQueryable<IGrouping<KQ, QG>> source,
        Expression<Func<QG, E>> elementShape,
        Expression<Func<IGrouping<KQ, QG>, IGrouping<K, E>>> shape) : base(source, shape)
    {
        _elementShape = elementShape;
    }

    protected override SequenceQueryShape<IGrouping<K, E>, IGrouping<KQ, QG>> WithSource(IQueryable<IGrouping<KQ, QG>> source) =>
        new GroupedQueryShape<K, KQ, E, QG, T, Q>(source, _elementShape, Shape);

    public override Expression<Func<IGrouping<KQ, QG>, R>> Translate<R>(Expression<Func<IGrouping<K, E>, R>> expression) =>
        GroupedShapeProjectionBuilder<K, KQ, E, QG, T, Q>.Translate(expression, _elementShape);

    public override IQueryShape Compose<R>(Expression<Func<IGrouping<K, E>, R>> outer) =>
        GroupedShapeProjectionBuilder<K, KQ, E, QG, T, Q>.Compose(Source, outer, _elementShape);
}
