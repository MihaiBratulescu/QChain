using System.Linq.Expressions;
using QChain.Visitors;

namespace QChain.Internal;

#if NET10_0_OR_GREATER
public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<(T, R?)> LeftJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey) =>
        LeftJoin(other, lKey, rKey, (l, r) => ValueTuple.Create(l, r));

    public IQuery<TResult> LeftJoin<R, K, TResult>(IQuery<R> other, 
                                                   Expression<Func<T, K>> leftKey, 
                                                   Expression<Func<R, K>> rightKey,
                                                   Expression<Func<T, R?, TResult>> resultSelector)
    {
        return LeftJoin((dynamic)other, leftKey, rightKey, resultSelector);
    }

    public IQuery<(T?, R)> RightJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey)=>
        RightJoin(other, lKey, rKey, (l, r) => ValueTuple.Create(l, r));

    public IQuery<TResult> RightJoin<R, K, TResult>(IQuery<R> other, 
                                                    Expression<Func<T, K>> leftKey, 
                                                    Expression<Func<R, K>> rightKey, 
                                                    Expression<Func<T?, R, TResult>> resultSelector)
    {
        return RightJoin((dynamic)other, leftKey, rightKey, resultSelector);
    }

    private IQuery<TResult> LeftJoin<R, RQ, K, TResult>(
        DeferredQuery<R, RQ> other,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R?, TResult>> resultSelector)
    {
        var source = Source.LeftJoin(
            other.Source,
            Translate(leftKey),
            other.Translate(rightKey),
            (l, r) => new Pair<Q, RQ?>
            {
                Left = l,
                Right = r
            });

        var shape = BuildJoinShape(
            Shape,
            other.Shape,
            resultSelector);

        return new DeferredQuery<TResult, Pair<Q, RQ?>>(source, shape);
    }

    private IQuery<TResult> RightJoin<R, RQ, K, TResult>(
        DeferredQuery<R, RQ> other,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T?, R, TResult>> resultSelector)
    {
        var source = Source.RightJoin(
            other.Source,
            Translate(leftKey),
            other.Translate(rightKey),
            (l, r) => new Pair<Q?, RQ>
            {
                Left = l,
                Right = r
            });

        var shape = BuildJoinShape(
            Shape,
            other.Shape,
            resultSelector);

        return new DeferredQuery<TResult, Pair<Q?, RQ>>(
            source,
            shape);
    }

    private static Expression<Func<Pair<LQ, RQ>, TResult>> BuildJoinShape<TL, LQ, TR, RQ, TResult>(
        Expression<Func<LQ, TL>> leftShape,
        Expression<Func<RQ, TR>> rightShape,
        Expression<Func<TL, TR, TResult>> selector)
    {
        var pair = Expression.Parameter(typeof(Pair<LQ, RQ>), "p");

        var leftQ = Expression.PropertyOrField(pair, nameof(Pair<LQ, RQ>.Left));
        var rightQ = Expression.PropertyOrField(pair, nameof(Pair<LQ, RQ>.Right));

        var left = new ReplaceExpressionVisitor(leftShape.Parameters[0], leftQ)
            .Visit(leftShape.Body)!;

        var right = new ReplaceExpressionVisitor(rightShape.Parameters[0], rightQ)
            .Visit(rightShape.Body)!;

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], left)
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], right)
            .Visit(body)!;

        return Expression.Lambda<Func<Pair<LQ, RQ>, TResult>>(body, pair);
    }
}
#endif
