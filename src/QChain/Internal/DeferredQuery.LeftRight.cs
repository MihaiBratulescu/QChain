using System.Linq.Expressions;
using QChain.Visitors;

namespace QChain.Internal;

#if NET10_0_OR_GREATER
public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<(T, R?)> LeftJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey)
    {
        var right = (DeferredQuery<R, R>)other; // or your internal typed interface

        var source = Source.LeftJoin(
            right.Source,
            Translate(lKey), right.Translate(rKey),
            (l, r) => new Pair<Q, R?> { Left = l, Right = r });

        return new DeferredQuery<(T, R?), Pair<Q, R?>>(
            source, BuildLeftJoinShape<R>());
    }

    public IQuery<(T?, R)> RightJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey)
    {
        var right = (DeferredQuery<R, R>)other;

        var source = Source.RightJoin(
            right.Source,
            Translate(lKey), right.Translate(rKey),
            (l, r) => new Pair<Q?, R> { Left = l, Right = r });

        return new DeferredQuery<(T?, R), Pair<Q?, R>>(
            source, BuildRightJoinShape<R>());
    }

    private Expression<Func<Pair<Q, R?>, (T, R?)>> BuildLeftJoinShape<R>()
    {
        var pair = Expression.Parameter(typeof(Pair<Q, R?>), "p");

        var left = Expression.PropertyOrField(pair, nameof(Pair<Q, R?>.Left));
        var right = Expression.PropertyOrField(pair, nameof(Pair<Q, R?>.Right));

        var publicLeft = ReplaceExpressionVisitor.Replace(
            Shape.Body, Shape.Parameters[0], left);

        var body = Expression.New(
            typeof(ValueTuple<T, R?>).GetConstructor([typeof(T), typeof(R)])!,
            publicLeft, right);

        return Expression.Lambda<Func<Pair<Q, R?>, (T, R?)>>(body, pair);
    }

    private Expression<Func<Pair<Q?, R>, (T?, R)>> BuildRightJoinShape<R>()
    {
        var pair = Expression.Parameter(typeof(Pair<Q?, R>), "p");

        var left = Expression.PropertyOrField(pair, nameof(Pair<Q?, R>.Left));
        var right = Expression.PropertyOrField(pair, nameof(Pair<Q?, R>.Right));

        var publicLeft = ReplaceExpressionVisitor.Replace(
            Shape.Body, Shape.Parameters[0], left);

        var body = Expression.New(
            typeof(ValueTuple<T?, R>).GetConstructor([typeof(T), typeof(R)])!,
            publicLeft, right);

        return Expression.Lambda<Func<Pair<Q?, R>, (T?, R)>>(body, pair);
    }
}
#endif
