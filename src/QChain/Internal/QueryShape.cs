using QChain.Visitors;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal interface IQueryShape
{
    IQueryable UntypedSource { get; }
    LambdaExpression UntypedShape { get; }
    Type SourceType { get; }
}

internal interface IUntypedQuery
{
    IQueryShape Untyped { get; }
}

internal sealed partial record QueryShape<T, Q>(IQueryable<Q> Source, Expression<Func<Q, T>> Shape) : IQueryShape
{
    public IQueryable UntypedSource => Source;
    public LambdaExpression UntypedShape => Shape;
    public Type SourceType => typeof(Q);

    public IQueryable<T> Project() => Source.Select(Shape);

    private QueryShape<T, Q> WithSource(IQueryable<Q> source) => new(source, Shape);

    public QueryShape<T, Q> Where(Expression<Func<T, bool>> predicate)
    {
        return WithSource(Source.Where(Translate(predicate)));
    }

    public QueryShape<T, Q> Skip(int count)
    {
        return WithSource(Source.Skip(count));
    }

    public QueryShape<T, Q> Take(int count)
    {
        return WithSource(Source.Take(count));
    }

    public QueryShape<T, Q> Page(int index, int count)
    {
        return WithSource(Source.Skip(index * count).Take(count));
    }

    public QueryShape<T, Q> OrderBy<K>(Expression<Func<T, K>> selector)
    {
        return WithSource(Source.OrderBy(Translate(selector)));
    }

    public QueryShape<T, Q> OrderByDescending<K>(Expression<Func<T, K>> selector)
    {
        return WithSource(Source.OrderByDescending(Translate(selector)));
    }

    public QueryShape<T, Q> ThenBy<K>(Expression<Func<T, K>> selector)
    {
        return WithSource(((IOrderedQueryable<Q>)Source).ThenBy(Translate(selector)));
    }

    public QueryShape<T, Q> ThenByDescending<K>(Expression<Func<T, K>> selector)
    {
        return WithSource(((IOrderedQueryable<Q>)Source).ThenByDescending(Translate(selector)));
    }

    public QueryShape<T, Q> Reverse()
    {
        return WithSource(Source.Reverse());
    }

    public IQueryShape Join<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R, TOut>> result)
    {
        return (IQueryShape)JoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(this, [right, leftKey, rightKey, result])!;
    }

    public IQueryShape GroupJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        return (IQueryShape)GroupJoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(this, [right, leftKey, rightKey, result])!;
    }

    public IQueryShape Distinct()
    {
        var lowered = TupleProjection<T, Q>.Lower(Shape.Body);

        return (IQueryShape)DistinctTypedMethod
            .MakeGenericMethod(lowered.Type)
            .Invoke(this, [lowered])!;
    }

    public Expression<Func<Q, TResult>> Translate<TResult>(Expression<Func<T, TResult>> expression) =>
        ComposeInternal(expression);

    public QueryShape<R, Q> Compose<R>(Expression<Func<T, R>> outer)
    {
        return new QueryShape<R, Q>(Source, ComposeInternal(outer));
    }

    public IQueryShape SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector)
    {
        var translated = Translate(collectionSelector);

        if (translated.Body is not MethodCallExpression call)
        {
            return new QueryShape<R, R>(
                Source.SelectMany(translated),
                item => item);
        }

        var source = call.Arguments[0];
        var itemShape = (LambdaExpression)call.Arguments[1];
        var elementType = itemShape.Parameters[0].Type;
        var collectionSelectorTyped = BuildCollectionSelector(translated, elementType, source);

        return (IQueryShape)SelectManyTypedMethod
            .MakeGenericMethod(typeof(R), elementType)
            .Invoke(this, [collectionSelectorTyped, itemShape])!;
    }

    public QueryShape<R, Pair<Q, C>> SelectMany<C, R>(
        Expression<Func<T, IEnumerable<C>>> collectionSelector,
        Expression<Func<T, C, R>> resultSelector)
    {
        var source = Source.SelectMany(
            Translate(collectionSelector),
            (q, c) => new Pair<Q, C>
            {
                Left = q,
                Right = c
            });

        return new QueryShape<R, Pair<Q, C>>(
            source,
            TranslateSelectManyResult(resultSelector));
    }

    private Expression<Func<Q, R>> ComposeInternal<R>(Expression<Func<T, R>> outer)
    {
        var body = ReplaceExpressionVisitor.Replace(outer.Body, outer.Parameters[0], Shape.Body);

        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Q, R>>(body, Shape.Parameters);
    }

    private static LambdaExpression BuildCollectionSelector(LambdaExpression selector, Type elementType, Expression body)
    {
        return Expression.Lambda(
            typeof(Func<,>).MakeGenericType(selector.Parameters[0].Type, typeof(IEnumerable<>).MakeGenericType(elementType)),
            body,
            selector.Parameters);
    }

    private QueryShape<R, QR> SelectManyTyped<R, QR>(
        LambdaExpression collectionSelectorUntyped,
        LambdaExpression itemShapeUntyped)
    {
        var collectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)collectionSelectorUntyped;
        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new QueryShape<R, QR>(
            Source.SelectMany(collectionSelector),
            itemShape);
    }

    private Expression<Func<Pair<Q, C>, R>> TranslateSelectManyResult<C, R>(
        Expression<Func<T, C, R>> resultSelector)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, C>), "p");

        var outerQ = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Left));
        var innerC = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Right));

        var publicShape = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], outerQ);

        var body = ReplaceExpressionVisitor.ReplaceMany(resultSelector.Body, new Dictionary<Expression, Expression>
        {
            [resultSelector.Parameters[0]] = publicShape,
            [resultSelector.Parameters[1]] = innerC
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Pair<Q, C>, R>>(body, pair);
    }

    private Expression<Func<TCarrier, T>> Rebuild<TCarrier>()
    {
        var carrier = Expression.Parameter(typeof(TCarrier), "x");

        return Expression.Lambda<Func<TCarrier, T>>(
            TupleProjection<T, TCarrier>.Rebuild(carrier, typeof(T)),
            carrier);
    }

    private QueryShape<T, TCarrier> DistinctTyped<TCarrier>(Expression lowered)
    {
        var carrierShape = Expression.Lambda<Func<Q, TCarrier>>(lowered, Shape.Parameters);
        var loweredShape = new QueryShape<T, TCarrier>(
            Source.Select(carrierShape),
            Rebuild<TCarrier>());

        return new QueryShape<T, TCarrier>(
            loweredShape.Source.Distinct(),
            loweredShape.Shape);
    }

    private QueryShape<TOut, Pair<Q, QR>> JoinTyped<R, K, TOut, QR>(
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var joined = Source.Join(
            right.Source,
            Translate(leftKey),
            right.Translate(rightKey),
            (left, rightRow) => new Pair<Q, QR>
            {
                Left = left,
                Right = rightRow
            });

        return new QueryShape<TOut, Pair<Q, QR>>(
            joined,
            BuildJoinShape(right.Shape, result));
    }

    private Expression<Func<Pair<Q, QR>, TOut>> BuildJoinShape<R, QR, TOut>(
        Expression<Func<QR, R>> rightShape,
        Expression<Func<T, R, TOut>> result)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, QR>), "p");

        var leftInternal = Expression.Property(pair, nameof(Pair<Q, QR>.Left));
        var rightInternal = Expression.Property(pair, nameof(Pair<Q, QR>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], leftInternal);
        var rightPublic = ReplaceExpressionVisitor.Replace(rightShape.Body, rightShape.Parameters[0], rightInternal);

        var body = ReplaceExpressionVisitor.ReplaceMany(result.Body, new Dictionary<Expression, Expression>
        {
            [result.Parameters[0]] = leftPublic,
            [result.Parameters[1]] = rightPublic
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Pair<Q, QR>, TOut>>(body, pair);
    }

    private QueryShape<TOut, Pair<Q, IEnumerable<QR>>> GroupJoinTyped<R, K, TOut, QR>(
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var grouped = Source.GroupJoin(
            right.Source,
            Translate(leftKey),
            right.Translate(rightKey),
            (left, rightRows) => new Pair<Q, IEnumerable<QR>>
            {
                Left = left,
                Right = rightRows
            });

        return new QueryShape<TOut, Pair<Q, IEnumerable<QR>>>(
            grouped,
            BuildGroupJoinShape(right.Shape, result));
    }

    private Expression<Func<Pair<Q, IEnumerable<QR>>, TOut>> BuildGroupJoinShape<R, QR, TOut>(
        Expression<Func<QR, R>> rightShape,
        Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, IEnumerable<QR>>), "p");

        var leftInternal = Expression.Property(pair, nameof(Pair<Q, IEnumerable<QR>>.Left));
        var rightInternal = Expression.Property(pair, nameof(Pair<Q, IEnumerable<QR>>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], leftInternal);
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

    private static readonly MethodInfo DistinctTypedMethod =
        typeof(QueryShape<T, Q>).GetMethod(nameof(DistinctTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo JoinTypedMethod =
        typeof(QueryShape<T, Q>).GetMethod(nameof(JoinTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo GroupJoinTypedMethod =
        typeof(QueryShape<T, Q>).GetMethod(nameof(GroupJoinTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo SelectManyTypedMethod =
        typeof(QueryShape<T, Q>).GetMethod(nameof(SelectManyTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));
}

#if NET10_0_OR_GREATER
internal sealed partial record QueryShape<T, Q>
{
    public IQueryShape LeftJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R?, TOut>> result)
    {
        return (IQueryShape)LeftJoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(this, [right, leftKey, rightKey, result])!;
    }

    public IQueryShape RightJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T?, R, TOut>> result)
    {
        return (IQueryShape)RightJoinTypedMethod
            .MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), right.SourceType)
            .Invoke(this, [right, leftKey, rightKey, result])!;
    }

    private QueryShape<TOut, Pair<Q, QR?>> LeftJoinTyped<R, K, TOut, QR>(
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R?, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var source = Source.LeftJoin(
            right.Source,
            Translate(leftKey),
            right.Translate(rightKey),
            (left, rightRow) => new Pair<Q, QR?>
            {
                Left = left,
                Right = rightRow
            });

        return new QueryShape<TOut, Pair<Q, QR?>>(
            source,
            BuildOuterJoinShape<Q, QR?, T, R?, TOut>(Shape, right.Shape, result));
    }

    private QueryShape<TOut, Pair<Q?, QR>> RightJoinTyped<R, K, TOut, QR>(
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T?, R, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var source = Source.RightJoin(
            right.Source,
            Translate(leftKey),
            right.Translate(rightKey),
            (left, rightRow) => new Pair<Q?, QR>
            {
                Left = left,
                Right = rightRow
            });

        return new QueryShape<TOut, Pair<Q?, QR>>(
            source,
            BuildOuterJoinShape<Q?, QR, T?, R, TOut>(Shape, right.Shape, result));
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
        typeof(QueryShape<T, Q>).GetMethod(nameof(LeftJoinTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo RightJoinTypedMethod =
        typeof(QueryShape<T, Q>).GetMethod(nameof(RightJoinTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
}
#endif

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
