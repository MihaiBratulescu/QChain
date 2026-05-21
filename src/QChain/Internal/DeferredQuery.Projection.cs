using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;
using ReferenceEqualityComparer = QChain.Visitors.ReferenceEqualityComparer;

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
    private IQuery<R> FlattenPreservingShape<R>(LambdaExpression translatedCollectionSelector)
    {
        if (translatedCollectionSelector.Body is not MethodCallExpression call)
            throw new NotSupportedException("SelectMany collection selector must be a method call.");

        // x => x.Item2.DefaultIfEmpty()
        if (call.Method.Name == nameof(Queryable.DefaultIfEmpty) ||
            call.Method.Name == nameof(Enumerable.DefaultIfEmpty))
        {
            var sourceExpression = call.Arguments[0];
            var elementType = sourceExpression.Type.GetGenericArguments()[0];

            var internalCollectionType = typeof(IEnumerable<>).MakeGenericType(elementType);

            var internalCollectionSelector = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(
                    translatedCollectionSelector.Parameters[0].Type,
                    internalCollectionType),
                sourceExpression,
                translatedCollectionSelector.Parameters);

            var identitySelectorParameter = Expression.Parameter(elementType, "x");

            var selectorExpression = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(elementType, elementType),
                identitySelectorParameter,
                identitySelectorParameter);

            var generic = FlattenPreservingShapeTypedMethod
                .MakeGenericMethod(typeof(R), elementType);

            return (IQuery<R>)generic.Invoke(
                this,
                [internalCollectionSelector, selectorExpression])!;
        }

        // x => x.Item2.Select(...)
        if (call.Arguments.Count >= 2)
        {
            var sourceExpression = call.Arguments[0];
            var selectorExpression = (LambdaExpression)StripQuote(call.Arguments[1]);

            var internalCollectionType = typeof(IEnumerable<>)
                .MakeGenericType(selectorExpression.Parameters[0].Type);

            var internalCollectionSelector = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(
                    translatedCollectionSelector.Parameters[0].Type,
                    internalCollectionType),
                sourceExpression,
                translatedCollectionSelector.Parameters);

            var generic = FlattenPreservingShapeTypedMethod
                .MakeGenericMethod(typeof(R), selectorExpression.Parameters[0].Type);

            return (IQuery<R>)generic.Invoke(
                this,
                [internalCollectionSelector, selectorExpression])!;
        }

        throw new NotSupportedException($"Unsupported SelectMany selector: {call}");
    }
    
    private static Expression StripQuote(Expression expression)
    {
        return expression.NodeType == ExpressionType.Quote
            ? ((UnaryExpression)expression).Operand
            : expression;
    }

    private static readonly MethodInfo FlattenPreservingShapeTypedMethod = typeof(DeferredQuery<T, Q>).GetMethod(nameof(FlattenPreservingShapeTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private DeferredQuery<R, QR> FlattenPreservingShapeTyped<R, QR>(LambdaExpression internalCollectionSelectorUntyped, LambdaExpression itemShapeUntyped)
    {
        var internalCollectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)internalCollectionSelectorUntyped;
        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new DeferredQuery<R, QR>(Source.SelectMany(internalCollectionSelector), itemShape);
    }

    private Expression<Func<Pair<Q, C>, R>> BuildSelectManyShape<C, R>(Expression<Func<T, C, R>> resultSelector)
    {
        var pair = Expression.Parameter(typeof(Pair<Q, C>), "p");

        var outerQ = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Left));
        var innerC = Expression.PropertyOrField(pair, nameof(Pair<Q, C>.Right));

        var outerT = ReplaceExpressionVisitor.Replace(
            Shape.Body,
            Shape.Parameters[0],
            outerQ);

        var body = ReplaceExpressionVisitor.Replace(
            resultSelector.Body,
            resultSelector.Parameters[0],
            outerT);

        body = ReplaceExpressionVisitor.Replace(
            body,
            resultSelector.Parameters[1],
            innerC);

        return Expression.Lambda<Func<Pair<Q, C>, R>>(body, pair);
    }

    private Expression<Func<Q, IEnumerable<C>>> TranslateSelectManyCollection<C>(
    Expression<Func<T, IEnumerable<C>>> collectionSelector)
    {
        var q = Expression.Parameter(typeof(Q), collectionSelector.Parameters[0].Name);

        var body = new SelectManyCollectionVisitor(
            collectionSelector.Parameters[0],
            Shape.Parameters[0],
            q)
            .Visit(collectionSelector.Body)!;

        return Expression.Lambda<Func<Q, IEnumerable<C>>>(body, q);
    }
    #endregion
}

internal sealed class SelectManyCollectionVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _publicParameter;
    private readonly ParameterExpression _shapeParameter;
    private readonly ParameterExpression _internalParameter;

    public SelectManyCollectionVisitor(
        ParameterExpression publicParameter,
        ParameterExpression shapeParameter,
        ParameterExpression internalParameter)
    {
        _publicParameter = publicParameter;
        _shapeParameter = shapeParameter;
        _internalParameter = internalParameter;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // x.Item1 / x.Item2
        if (node.Expression == _publicParameter)
        {
            return TranslateTupleMember(node.Member.Name);
        }

        return base.VisitMember(node);
    }

    private Expression TranslateTupleMember(string name)
    {
        // public tuple Item1 maps to Shape.Body.Item1
        // Shape usually maps Pair.Left / Pair.Right internally.

        return name switch
        {
            "Item1" => Expression.PropertyOrField(_internalParameter, "Left"),
            "Item2" => Expression.PropertyOrField(_internalParameter, "Right"),
            _ => throw new NotSupportedException($"Unsupported tuple member: {name}")
        };
    }
}
