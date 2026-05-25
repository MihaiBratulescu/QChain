#if NET10_0_OR_GREATER
using QChain.Internal;
using QChain.Visitors;

using System.Linq.Expressions;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
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
        Query<R, RQ> other,
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

        return new Query<TResult, Pair<Q, RQ?>>(source, shape);
    }

    private IQuery<TResult> RightJoin<R, RQ, K, TResult>(
        Query<R, RQ> other,
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

        return new Query<TResult, Pair<Q?, RQ>>(
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

        var left = ReplaceExpressionVisitor.Replace(leftShape.Body, leftShape.Parameters[0], leftQ);

        var right = ReplaceExpressionVisitor.Replace(rightShape.Body, rightShape.Parameters[0], rightQ);

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = left,
            [selector.Parameters[1]] = right
        });

        return Expression.Lambda<Func<Pair<LQ, RQ>, TResult>>(body, pair);
    }
}
#endif
