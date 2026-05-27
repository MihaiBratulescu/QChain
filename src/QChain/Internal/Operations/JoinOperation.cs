using QChain.Internal.Helpers;
using QChain.Internal.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal static class JoinOperation<T, Q>
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
        typeof(JoinOperation<T, Q>).GetMethod(nameof(LeftJoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo RightJoinTypedMethod =
        typeof(JoinOperation<T, Q>).GetMethod(nameof(RightJoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
#endif

    private static readonly MethodInfo JoinTypedMethod =
        typeof(JoinOperation<T, Q>).GetMethod(nameof(JoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo GroupJoinTypedMethod =
        typeof(JoinOperation<T, Q>).GetMethod(nameof(GroupJoinTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));
}
