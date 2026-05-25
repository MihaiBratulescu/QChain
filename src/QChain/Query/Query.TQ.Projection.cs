using QChain.Internal;
using QChain.Visitors;

using System.Linq.Expressions;
using System.Reflection;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public virtual IQuery<R> Select<R>(Expression<Func<T, R>> mapping) =>
       new Query<R, Q>(Source, Compose(mapping, Shape));

    public IQuery<R> SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
        FlattenPreservingShape<R>(Translate(collectionSelector));

    public IQuery<R> SelectMany<C, R>(Expression<Func<T, IEnumerable<C>>> collectionSelector,
                                              Expression<Func<T, C, R>> resultSelector)
    {
        var source = Source.SelectMany(
            TranslateSelectManyCollection(collectionSelector),
            (q, c) => new Pair<Q, C> { Left = q, Right = c });

        return new Query<R, Pair<Q, C>>(source, TranslateSelectManyResult(resultSelector));
    }

    #region Helpers
    private static readonly MethodInfo FlattenPreservingShapeTypedMethod = typeof(Query<T, Q>).GetMethod(nameof(FlattenPreservingShapeTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private IQuery<R> FlattenPreservingShape<R>(LambdaExpression translatedCollectionSelector)
    {
        var call = (MethodCallExpression)translatedCollectionSelector.Body;

        return FlattenSelect<R>(translatedCollectionSelector, call);
    }

    private IQuery<R> FlattenSelect<R>(LambdaExpression selector, MethodCallExpression call)
    {
        var source = call.Arguments[0];
        var itemShape = (LambdaExpression)call.Arguments[1];
        var elementType = itemShape.Parameters[0].Type;

        return InvokeFlatten<R>(elementType, 
            BuildCollectionSelector(selector, elementType, source), itemShape);
    }

    private static LambdaExpression BuildCollectionSelector(LambdaExpression selector, Type elementType, Expression body)
    {
        return Expression.Lambda(
            typeof(Func<,>).MakeGenericType(selector.Parameters[0].Type, typeof(IEnumerable<>).MakeGenericType(elementType)),
            body, selector.Parameters);
    }

    private IQuery<R> InvokeFlatten<R>(Type elementType, LambdaExpression collectionSelector, LambdaExpression itemShape)
    {
        var generic = FlattenPreservingShapeTypedMethod
            .MakeGenericMethod(typeof(R), elementType);

        return (IQuery<R>)generic.Invoke(this, [collectionSelector, itemShape])!;
    }

    private Query<R, QR> FlattenPreservingShapeTyped<R, QR>(LambdaExpression collectionSelectorUntyped, LambdaExpression itemShapeUntyped)
    {
        var collectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)collectionSelectorUntyped;

        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new Query<R, QR>(Source.SelectMany(collectionSelector), itemShape);
    }

    private Expression<Func<Q, IEnumerable<C>>> TranslateSelectManyCollection<C>(Expression<Func<T, IEnumerable<C>>> collectionSelector)
        => Translate(collectionSelector);

    private Expression<Func<Pair<Q, C>, R>> TranslateSelectManyResult<C, R>(Expression<Func<T, C, R>> resultSelector)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, C>), "p");

        var outerQ = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Left));
        var innerC = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Right));

        var publicShape = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], outerQ);

        var body = ReplaceExpressionVisitor.ReplaceMany(resultSelector.Body, new Dictionary<Expression, Expression>
        {
            [resultSelector.Parameters[0]] = publicShape,
            [resultSelector.Parameters[1]] = innerC
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Pair<Q, C>, R>>(body, pair);
    }
    #endregion
}
