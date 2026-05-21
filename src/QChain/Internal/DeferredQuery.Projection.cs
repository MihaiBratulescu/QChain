using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<R> Select<R>(Expression<Func<T, R>> mapping) =>
       new DeferredQuery<R, Q>(Source, Compose(mapping, Shape));

    public IQuery<R> SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
        FlattenPreservingShape<R>(Translate(collectionSelector));

    public IQuery<R> SelectMany<C, R>(Expression<Func<T, IEnumerable<C>>> collectionSelector,
                                      Expression<Func<T, C, R>> resultSelector)
    {
        var source = Source.SelectMany(
            TranslateSelectManyCollection(collectionSelector),
            (q, c) => new Pair<Q, C> { Left = q, Right = c });

        return new DeferredQuery<R, Pair<Q, C>>(source, BuildSelectManyShape(resultSelector));
    }

    #region Helpers
    private static readonly MethodInfo FlattenPreservingShapeTypedMethod = typeof(DeferredQuery<T, Q>).GetMethod(nameof(FlattenPreservingShapeTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private IQuery<R> FlattenPreservingShape<R>(LambdaExpression translatedCollectionSelector)
    {
        if (translatedCollectionSelector.Body is not MethodCallExpression call)
            throw new NotSupportedException(
                "SelectMany collection selector must be a method call.");

        return call.Method.Name switch
        {
            nameof(Queryable.DefaultIfEmpty) or nameof(Enumerable.DefaultIfEmpty)
                => FlattenDefaultIfEmpty<R>(translatedCollectionSelector, call),

            _ when call.Arguments.Count >= 2
                => FlattenSelect<R>(translatedCollectionSelector, call),

            _ => throw new NotSupportedException(
                $"Unsupported SelectMany selector: {call}")
        };
    }

    private IQuery<R> FlattenDefaultIfEmpty<R>(LambdaExpression selector, MethodCallExpression call)
    {
        var source = call.Arguments[0];
        var elementType = source.Type.GetGenericArguments()[0];

        var collectionSelector = BuildCollectionSelector(
            selector.Parameters[0].Type,
            elementType,
            source,
            selector.Parameters);

        var item = Expression.Parameter(elementType, "x");

        var itemShape = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(elementType, elementType),
            item,
            item);

        return InvokeFlatten<R>(elementType, collectionSelector, itemShape);
    }

    private IQuery<R> FlattenSelect<R>(LambdaExpression selector, MethodCallExpression call)
    {
        var source = call.Arguments[0];
        var itemShape = (LambdaExpression)StripQuote(call.Arguments[1]);
        var elementType = itemShape.Parameters[0].Type;

        var collectionSelector = BuildCollectionSelector(
            selector.Parameters[0].Type,
            elementType,
            source,
            selector.Parameters);

        return InvokeFlatten<R>(elementType, collectionSelector, itemShape);
    }

    private static LambdaExpression BuildCollectionSelector(Type sourceType, Type elementType, Expression body, IReadOnlyList<ParameterExpression> parameters)
    {
        return Expression.Lambda(typeof(Func<,>).MakeGenericType(
            sourceType, typeof(IEnumerable<>).MakeGenericType(elementType)), body, parameters);
    }

    private IQuery<R> InvokeFlatten<R>(Type elementType, LambdaExpression collectionSelector, LambdaExpression itemShape)
    {
        var generic = FlattenPreservingShapeTypedMethod
            .MakeGenericMethod(typeof(R), elementType);

        return (IQuery<R>)generic.Invoke(this, [collectionSelector, itemShape])!;
    }

    private DeferredQuery<R, QR> FlattenPreservingShapeTyped<R, QR>(LambdaExpression collectionSelectorUntyped, LambdaExpression itemShapeUntyped)
    {
        var collectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)collectionSelectorUntyped;

        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new DeferredQuery<R, QR>(Source.SelectMany(collectionSelector), itemShape);
    }

    private static Expression StripQuote(Expression expression) =>
        expression.NodeType == ExpressionType.Quote
            ? ((UnaryExpression)expression).Operand
            : expression;

    private Expression<Func<Q, IEnumerable<C>>> TranslateSelectManyCollection<C>(Expression<Func<T, IEnumerable<C>>> collectionSelector)
    {
        var q = Expression.Parameter(typeof(Q), collectionSelector.Parameters[0].Name);

        var publicShape = ReplaceExpressionVisitor.Replace(
            Shape.Body, Shape.Parameters[0], q);

        var body = new ProjectionInliningVisitor(
                collectionSelector.Parameters[0], publicShape)
            .Visit(collectionSelector.Body)!;

        return Expression.Lambda<Func<Q, IEnumerable<C>>>(body, q);
    }

    private Expression<Func<Pair<Q, C>, R>> BuildSelectManyShape<C, R>(Expression<Func<T, C, R>> resultSelector)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, C>), "p");

        var outerQ = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Left));
        var innerC = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Right));

        var publicShape = ReplaceExpressionVisitor.Replace(
            Shape.Body, Shape.Parameters[0], outerQ);

        var body = new ProjectionInliningVisitor(
                resultSelector.Parameters[0], publicShape)
            .Visit(resultSelector.Body)!;

        body = ReplaceExpressionVisitor.Replace(
            body, resultSelector.Parameters[1], innerC);

        return Expression.Lambda<Func<Pair<Q, C>, R>>(body, pair);
    }
    #endregion
}
