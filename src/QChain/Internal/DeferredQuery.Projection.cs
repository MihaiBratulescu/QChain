using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<R> Select<R>(Expression<Func<T, R>> mapping) =>
       new DeferredQuery<R, Q>(Source, Compose(mapping, Shape));

    public IQuery<R> SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
        FlattenPreservingShape<R>(Translate(collectionSelector));

    #region Helpers
    private IQuery<R> FlattenPreservingShape<R>(LambdaExpression translatedCollectionSelector)
    {
        var call = translatedCollectionSelector.Body as MethodCallExpression;

        var sourceExpression = call.Arguments[0];
        var selectorExpression = (LambdaExpression)call.Arguments[1];

        var internalCollectionType = typeof(IEnumerable<>).MakeGenericType(selectorExpression.Parameters[0].Type);
        var internalCollectionSelector = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(translatedCollectionSelector.Parameters[0].Type, internalCollectionType),
            sourceExpression,
            translatedCollectionSelector.Parameters);


        var generic = FlattenPreservingShapeTypedMethod.MakeGenericMethod(typeof(R), selectorExpression.Parameters[0].Type);

        return (IQuery<R>)generic.Invoke(this, [internalCollectionSelector, selectorExpression])!;
    }

    private static readonly MethodInfo FlattenPreservingShapeTypedMethod = typeof(DeferredQuery<T, Q>).GetMethod(nameof(FlattenPreservingShapeTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private DeferredQuery<R, QR> FlattenPreservingShapeTyped<R, QR>(LambdaExpression internalCollectionSelectorUntyped, LambdaExpression itemShapeUntyped)
    {
        var internalCollectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)internalCollectionSelectorUntyped;
        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new DeferredQuery<R, QR>(Source.SelectMany(internalCollectionSelector), itemShape);
    }
    #endregion
}
