#if NET10_0_OR_GREATER

using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

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
