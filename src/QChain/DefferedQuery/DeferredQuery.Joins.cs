using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<(T, R)> Join<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey) =>
        Join(right, lKey, rKey, (left, rightRow) => new ValueTuple<T, R>(left, rightRow));

    public IQuery<TOut> Join<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result) =>
        JoinInternal((right as IInternalQuery)!, lKey, rKey, result);

    public IQuery<(T, IEnumerable<R>)> GroupJoin<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey) =>
        GroupJoin(right, lKey, rKey, (t, items) => new ValueTuple<T, IEnumerable<R>>(t, items));

    public IQuery<TOut> GroupJoin<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result) =>
        GroupJoinInternal((right as IInternalQuery)!, lKey, rKey, result);

    #region Helpers
    private IQuery<TOut> JoinInternal<R, K, TOut>(IInternalQuery right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result)
    {
        var qr = right.UntypedShape.Parameters[0].Type;

        var generic = JoinInternalTypedMethod.MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), qr);

        return (IQuery<TOut>)generic.Invoke(this, [right, lKey, rKey, result])!;
    }

    private IQuery<TOut> GroupJoinInternal<R, K, TOut>(IInternalQuery right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var qr = right.UntypedShape.Parameters[0].Type;

        var generic = GroupJoinInternalTypedMethod.MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), qr);

        return (IQuery<TOut>)generic.Invoke(this, [right, lKey, rKey, result])!;
    }

    private static readonly MethodInfo JoinInternalTypedMethod = typeof(DeferredQuery<T, Q>).GetMethod(nameof(JoinInternalTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo GroupJoinInternalTypedMethod = typeof(DeferredQuery<T, Q>).GetMethod(nameof(GroupJoinInternalTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private DeferredQuery<TOut, Pair<Q, QR>> JoinInternalTyped<R, K, TOut, QR>(IInternalQuery rightUntyped, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result)
    {
        var right = (DeferredQuery<R, QR>)rightUntyped;

        var joined = Source.Join(right.Source, Translate(lKey), right.Translate(rKey),
            (l, r) => new Pair<Q, QR> { Left = l, Right = r });

        return new DeferredQuery<TOut, Pair<Q, QR>>(joined, BuildJoinShape(right.Shape, result));
    }

    private Expression<Func<Pair<Q, QR>, TOut>> BuildJoinShape<R, QR, TOut>(Expression<Func<QR, R>> rightShape, Expression<Func<T, R, TOut>> result)
    {
        var pairParam = Expression.Parameter(typeof(Pair<Q, QR>), "p");

        var leftInternal = Expression.Property(pairParam, nameof(Pair<Q, QR>.Left));
        var rightInternal = Expression.Property(pairParam, nameof(Pair<Q, QR>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], leftInternal);

        var rightPublic = ReplaceExpressionVisitor.Replace(rightShape.Body, rightShape.Parameters[0], rightInternal);

        var body = ReplaceExpressionVisitor.ReplaceMany(result.Body, new Dictionary<Expression, Expression>
        {
            [result.Parameters[0]] = leftPublic,
            [result.Parameters[1]] = rightPublic
        });

        return Expression.Lambda<Func<Pair<Q, QR>, TOut>>(body, pairParam);
    }

    private DeferredQuery<TOut, Pair<Q, IEnumerable<QR>>> GroupJoinInternalTyped<R, K, TOut, QR>(IInternalQuery rightUntyped, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var right = (DeferredQuery<R, QR>)rightUntyped;

        var grouped = Source.GroupJoin(right.Source, Translate(lKey), right.Translate(rKey),
            (l, r) => new Pair<Q, IEnumerable<QR>> { Left = l, Right = r });

        return new DeferredQuery<TOut, Pair<Q, IEnumerable<QR>>>(grouped, BuildGroupJoinShape(right.Shape, result));
    }

    private Expression<Func<Pair<Q, IEnumerable<QR>>, TOut>> BuildGroupJoinShape<R, QR, TOut>(Expression<Func<QR, R>> rightShape, Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var pairParam = Expression.Parameter(typeof(Pair<Q, IEnumerable<QR>>), "p");

        var leftInternal = Expression.Property(pairParam, nameof(Pair<Q, IEnumerable<QR>>.Left));
        var rightInternal = Expression.Property(pairParam, nameof(Pair<Q, IEnumerable<QR>>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], leftInternal);

        var projectedRight = ComposeEnumerable(rightShape, rightInternal);

        var body = ReplaceExpressionVisitor.ReplaceMany(result.Body, new Dictionary<Expression, Expression>
        {
            [result.Parameters[0]] = leftPublic,
            [result.Parameters[1]] = projectedRight
        });

        return Expression.Lambda<Func<Pair<Q, IEnumerable<QR>>, TOut>>(body, pairParam);
    }

    private static MethodCallExpression ComposeEnumerable<TRightInternal, TRightPublic>(Expression<Func<TRightInternal, TRightPublic>> itemShape, Expression enumerableExpression)
    {
        return Expression.Call(
            EnumerableSelectMethod.MakeGenericMethod(typeof(TRightInternal), typeof(TRightPublic)),
            enumerableExpression,
            itemShape);
    }

    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));

    #endregion
}
