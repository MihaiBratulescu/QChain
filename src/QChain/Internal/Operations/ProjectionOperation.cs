using QChain.Internal.Helpers;
using QChain.Internal.Shapes;
using QChain.Internal.Visitors;
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
        typeof(ProjectionOperation<T, Q>).GetMethod(nameof(SelectManyTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
}
