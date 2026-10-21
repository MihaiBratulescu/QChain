using PCompose.Visitors;
using QChain.Internal.Helpers;
using QChain.Internal.Shapes;
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
            .InvokeUnwrapped(null, [left, right, leftKey, rightKey, result])!;
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
            .InvokeUnwrapped(null, [left, right, leftKey, rightKey, result])!;
    }

    private static IQueryShape JoinTyped<R, K, TOut, QR>(
        SequenceQueryShape<T, Q> left,
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var (pack, shape) = CarrierProjection.Combine(left.Shape, right.Shape, result);
        var joined = left.Source.Provider.CreateQuery(Expression.Call(
            typeof(Queryable), nameof(Queryable.Join), [typeof(Q), typeof(QR), typeof(K), pack.ReturnType],
            left.Source.Expression, right.Source.Expression,
            Expression.Quote(left.Translate(leftKey)), Expression.Quote(right.Translate(rightKey)), Expression.Quote(pack)));

        return (IQueryShape)Activator.CreateInstance(
            typeof(JoinedQueryShape<,>).MakeGenericType(typeof(TOut), pack.ReturnType), joined, shape)!;
    }

    private static IQueryShape GroupJoinTyped<R, K, TOut, QR>(
        SequenceQueryShape<T, Q> left,
        IQueryShape rightUntyped,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var right = (QueryShape<R, QR>)rightUntyped;

        var rows = Expression.Parameter(typeof(IEnumerable<QR>), "rows");
        var rightRows = Expression.Lambda(ComposeEnumerable(right.Shape, rows), rows);
        var (pack, shape) = CarrierProjection.Combine(left.Shape, rightRows, result);
        var grouped = left.Source.Provider.CreateQuery(Expression.Call(
            typeof(Queryable), nameof(Queryable.GroupJoin), [typeof(Q), typeof(QR), typeof(K), pack.ReturnType],
            left.Source.Expression, right.Source.Expression,
            Expression.Quote(left.Translate(leftKey)), Expression.Quote(right.Translate(rightKey)), Expression.Quote(pack)));

        return (IQueryShape)Activator.CreateInstance(
            typeof(JoinedQueryShape<,>).MakeGenericType(typeof(TOut), pack.ReturnType), grouped, shape)!;
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
            .InvokeUnwrapped(null, [left, right, leftKey, rightKey, result])!;
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
            .InvokeUnwrapped(null, [left, right, leftKey, rightKey, result])!;
    }

    private static SequenceQueryShape<TOut, Carrier<Q, QR?>> LeftJoinTyped<R, K, TOut, QR>(
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
            (leftRow, rightRow) => new Carrier<Q, QR?>
            {
                Item1 = leftRow,
                Item2 = rightRow
            });

        return new JoinedQueryShape<TOut, Carrier<Q, QR?>>(
            joined,
            BuildOuterJoinShape<Q, QR?, T, R?, TOut>(left.Shape, right.Shape, result));
    }

    private static SequenceQueryShape<TOut, Carrier<Q?, QR>> RightJoinTyped<R, K, TOut, QR>(
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
            (leftRow, rightRow) => new Carrier<Q?, QR>
            {
                Item1 = leftRow,
                Item2 = rightRow
            });

        return new JoinedQueryShape<TOut, Carrier<Q?, QR>>(
            joined,
            BuildOuterJoinShape<Q?, QR, T?, R, TOut>(left.Shape, right.Shape, result));
    }

    private static Expression<Func<Carrier<TLeftSource, TRightSource>, TOut>> BuildOuterJoinShape<TLeftSource, TRightSource, TLeft, TRight, TOut>(
        LambdaExpression leftShape,
        LambdaExpression rightShape,
        Expression<Func<TLeft, TRight, TOut>> selector)
    {
        var pair = Expression.Parameter(typeof(Carrier<TLeftSource, TRightSource>), "p");

        var leftQ = Expression.PropertyOrField(pair, nameof(Carrier<TLeftSource, TRightSource>.Item1));
        var rightQ = Expression.PropertyOrField(pair, nameof(Carrier<TLeftSource, TRightSource>.Item2));

        var left = ReplaceExpressionVisitor.Replace(leftShape.Body, leftShape.Parameters[0], leftQ);
        var right = ReplaceExpressionVisitor.Replace(rightShape.Body, rightShape.Parameters[0], rightQ);

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = left,
            [selector.Parameters[1]] = right
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Carrier<TLeftSource, TRightSource>, TOut>>(body, pair);
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
