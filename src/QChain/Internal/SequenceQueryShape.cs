using QChain.Internal.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal abstract partial class SequenceQueryShape<T, Q>(
    IQueryable<Q> source,
    Expression<Func<Q, T>> shape) : QueryShape<T, Q>(source, shape)
{
    public SequenceQueryShape<T, Q> Where(Expression<Func<T, bool>> predicate) =>
        WithSource(Source.Where(Translate(predicate)));

    public SequenceQueryShape<T, Q> Skip(int count) =>
        WithSource(Source.Skip(count));

    public SequenceQueryShape<T, Q> Take(int count) =>
        WithSource(Source.Take(count));

    public SequenceQueryShape<T, Q> Page(int index, int count) =>
        WithSource(Source.Skip(index * count).Take(count));

    public SequenceQueryShape<T, Q> OrderBy<K>(Expression<Func<T, K>> selector) =>
        WithSource(Source.OrderBy(Translate(selector)));

    public SequenceQueryShape<T, Q> OrderByDescending<K>(Expression<Func<T, K>> selector) =>
        WithSource(Source.OrderByDescending(Translate(selector)));

    public SequenceQueryShape<T, Q> ThenBy<K>(Expression<Func<T, K>> selector) =>
        WithSource(((IOrderedQueryable<Q>)Source).ThenBy(Translate(selector)));

    public SequenceQueryShape<T, Q> ThenByDescending<K>(Expression<Func<T, K>> selector) =>
        WithSource(((IOrderedQueryable<Q>)Source).ThenByDescending(Translate(selector)));

    public SequenceQueryShape<T, Q> Reverse() => WithSource(Source.Reverse());

    public IQueryShape Select<R>(Expression<Func<T, R>> mapping) =>
        Compose(mapping);

    public virtual IQueryShape Compose<R>(Expression<Func<T, R>> outer) =>
        new ProjectedQueryShape<R, Q>(Source, ComposeInternal(outer));

    public IQueryShape SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector)
    {
        var translated = Translate(collectionSelector);

        if (translated.Body is not MethodCallExpression call)
        {
            return new ProjectedQueryShape<R, R>(
                Source.SelectMany(translated),
                item => item);
        }

        var source = call.Arguments[0];
        var itemShape = (LambdaExpression)call.Arguments[1];
        var elementType = itemShape.Parameters[0].Type;
        var collectionSelectorTyped = BuildCollectionSelector(translated, elementType, source);

        return (IQueryShape)SelectManyTypedMethod
            .MakeGenericMethod(typeof(R), elementType)
            .Invoke(this, [collectionSelectorTyped, itemShape])!;
    }

    public SequenceQueryShape<R, Pair<Q, C>> SelectMany<C, R>(
        Expression<Func<T, IEnumerable<C>>> collectionSelector,
        Expression<Func<T, C, R>> resultSelector)
    {
        var source = Source.SelectMany(
            Translate(collectionSelector),
            (q, c) => new Pair<Q, C>
            {
                Left = q,
                Right = c
            });

        return new ProjectedQueryShape<R, Pair<Q, C>>(
            source,
            TranslateSelectManyResult(resultSelector));
    }

    public IQueryShape Join<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R, TOut>> result) =>
        JoinShapeBuilder<T, Q>.Join(this, right, leftKey, rightKey, result);

    public IQueryShape GroupJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, IEnumerable<R>, TOut>> result) =>
        JoinShapeBuilder<T, Q>.GroupJoin(this, right, leftKey, rightKey, result);

    public IQueryShape Distinct() =>
        SetShapeBuilder<T, Q>.Distinct(this);

    public IQueryShape Union(IQueryShape other) =>
        SetShapeBuilder<T, Q>.Union(this, other);

    public IQueryShape Concat(IQueryShape other) =>
        SetShapeBuilder<T, Q>.Concat(this, other);

    public IQueryShape Except(IQueryShape other) =>
        SetShapeBuilder<T, Q>.Except(this, other);

    public IQueryShape Intersect(IQueryShape other) =>
        SetShapeBuilder<T, Q>.Intersect(this, other);

    public SequenceQueryShape<T, Q> ExceptBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector) =>
        WhereKeyIn(keys, keySelector, include: false);

    public SequenceQueryShape<T, Q> IntersectBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector) =>
        WhereKeyIn(keys, keySelector, include: true);

    protected abstract SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source);

    private SequenceQueryShape<T, Q> WhereKeyIn<K>(
        IEnumerable<K> keys,
        Expression<Func<T, K>> keySelector,
        bool include)
    {
        var translated = Translate(keySelector);

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(K)],
            Expression.Constant(keys),
            translated.Body);

        Expression body = include ? contains : Expression.Not(contains);

        var predicate = Expression.Lambda<Func<Q, bool>>(
            body,
            translated.Parameters);

        return WithSource(Source.Where(predicate));
    }

    private static LambdaExpression BuildCollectionSelector(LambdaExpression selector, Type elementType, Expression body)
    {
        return Expression.Lambda(
            typeof(Func<,>).MakeGenericType(selector.Parameters[0].Type, typeof(IEnumerable<>).MakeGenericType(elementType)),
            body,
            selector.Parameters);
    }

    private SequenceQueryShape<R, QR> SelectManyTyped<R, QR>(
        LambdaExpression collectionSelectorUntyped,
        LambdaExpression itemShapeUntyped)
    {
        var collectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)collectionSelectorUntyped;
        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new ProjectedQueryShape<R, QR>(
            Source.SelectMany(collectionSelector),
            itemShape);
    }

    private Expression<Func<Pair<Q, C>, R>> TranslateSelectManyResult<C, R>(
        Expression<Func<T, C, R>> resultSelector)
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

    private static readonly MethodInfo SelectManyTypedMethod =
        typeof(SequenceQueryShape<T, Q>).GetMethod(nameof(SelectManyTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
}
