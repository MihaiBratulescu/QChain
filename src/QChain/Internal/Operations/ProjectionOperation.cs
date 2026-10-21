using QChain.Internal.Helpers;
using QChain.Internal.Shapes;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal static class ProjectionOperation<T, Q>
{
    public static IQueryShape Compose<R>(SequenceQueryShape<T, Q> source, Expression<Func<T, R>> outer) =>
        new ProjectedQueryShape<R, Q>(source.Source, source.Translate(outer));

    public static IQueryShape SelectMany<R>(
        SequenceQueryShape<T, Q> source,
        Expression<Func<T, IEnumerable<R>>> collectionSelector)
    {
        var translated = source.Translate(collectionSelector);
        if (!TryExtractProjection(translated, out var collection, out var itemShape))
            return new ProjectedQueryShape<R, R>(source.Source.SelectMany(translated), item => item);

        return (IQueryShape)SelectManyTypedMethod.MakeGenericMethod(typeof(R), itemShape.Parameters[0].Type)
            .InvokeUnwrapped(null, [source, collection, itemShape]);
    }

    public static IQueryShape SelectMany<C, R>(
        SequenceQueryShape<T, Q> source,
        Expression<Func<T, IEnumerable<C>>> collectionSelector,
        Expression<Func<T, C, R>> resultSelector)
    {
        var translated = source.Translate(collectionSelector);
        if (TryExtractProjection(translated, out var collection, out var itemShape))
            return (IQueryShape)SelectManyResultTypedMethod.MakeGenericMethod(typeof(C), typeof(R), itemShape.Parameters[0].Type)
                .InvokeUnwrapped(null, [source, collection, itemShape, resultSelector]);

        Expression<Func<C, C>> identity = item => item;
        return SelectManyResultTyped(source, translated, identity, resultSelector);
    }

    private static bool TryExtractProjection(LambdaExpression selector, out LambdaExpression collection, out LambdaExpression itemShape)
    {
        collection = itemShape = null!;
        if (selector.Body is not MethodCallExpression call || call.Method.DeclaringType != typeof(Enumerable) ||
            call.Method.Name != nameof(Enumerable.Select) ||
            call.Arguments[1] is not LambdaExpression { Parameters.Count: 1 } projection)
            return false;

        var outer = new ParameterFindingVisitor(selector.Parameters[0]);
        outer.Visit(projection.Body);
        if (outer.Found)
            return false;

        itemShape = projection;
        collection = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(Q), typeof(IEnumerable<>).MakeGenericType(projection.Parameters[0].Type)),
            call.Arguments[0], selector.Parameters);
        return true;
    }

    private static IQueryShape SelectManyResultTyped<C, R, QR>(
        SequenceQueryShape<T, Q> source,
        LambdaExpression collection,
        Expression<Func<QR, C>> itemShape,
        Expression<Func<T, C, R>> resultSelector)
    {
        var (pack, shape) = CarrierProjection.Combine(source.Shape, itemShape, resultSelector);
        var flattened = source.Source.Provider.CreateQuery(Expression.Call(
            typeof(Queryable), nameof(Queryable.SelectMany), [typeof(Q), typeof(QR), pack.ReturnType],
            source.Source.Expression, Expression.Quote(collection), Expression.Quote(pack)));

        return (IQueryShape)Activator.CreateInstance(
            typeof(ProjectedQueryShape<,>).MakeGenericType(typeof(R), pack.ReturnType), flattened, shape)!;
    }

    private static SequenceQueryShape<R, QR> SelectManyTyped<R, QR>(
        SequenceQueryShape<T, Q> source,
        LambdaExpression collection,
        Expression<Func<QR, R>> itemShape) =>
        new ProjectedQueryShape<R, QR>(source.Source.SelectMany((Expression<Func<Q, IEnumerable<QR>>>)collection), itemShape);

    private sealed class ParameterFindingVisitor(ParameterExpression parameter) : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found |= node == parameter;
            return node;
        }
    }

    private static readonly MethodInfo SelectManyTypedMethod = typeof(ProjectionOperation<T, Q>)
        .GetMethod(nameof(SelectManyTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo SelectManyResultTypedMethod = typeof(ProjectionOperation<T, Q>)
        .GetMethod(nameof(SelectManyResultTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
}
