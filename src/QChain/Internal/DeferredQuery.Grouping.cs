using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    //IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>
    public IQuery<IGrouping<K, T>> GroupBy<K>(Expression<Func<T, K>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return RawGroupedQueryBuilder<T, Q>.Create(
            Source,
            TranslateGroupingKey(selector),
            element,
            Shape);
    }

    //IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>
    public IQuery<IGrouping<K, E>> GroupBy<K, E>(Expression<Func<T, K>> selector, Expression<Func<T, E>> elementSelector)
    {
        return RawGroupedQueryBuilder<T, Q>.Create(
            Source,
            TranslateGroupingKey(selector),
            TranslateGroupingElement(elementSelector),
            e => e);
    }

    //NEW
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            Source,
            Translate(key),
            element,
            TranslateGroup(selector));
    }

    //IQueryable<TResult> GroupBy<TSource, TKey, TResult>
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<K, IEnumerable<T>, R>> resultsSelector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            Source,
            Translate(key),
            element,
            TranslateInternalElementGroup(resultsSelector));
    }

    //IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>
    public IQuery<R> GroupBy<K, E, R>(Expression<Func<T, K>> key, Expression<Func<T, E>> elementSelector, Expression<Func<K, IEnumerable<E>, R>> resultsSelector)
    {
        var translatedKey = Translate(key);
        var translatedElement = Translate(elementSelector);
        var shape = TranslateElementGroup(resultsSelector);

        return ProjectedGroupQueryBuilder<T, Q>.Create(Source, translatedKey, translatedElement, shape);
    }

    #region Helpers
    private Expression<Func<Q, K>> TranslateGroupingKey<K>(Expression<Func<T, K>> expression)
    {
        var body = new ProjectionInliningVisitor(expression.Parameters[0], Shape.Body).Visit(expression.Body)!;
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Q, K>>(body, Shape.Parameters);
    }

    private Expression<Func<Q, E>> TranslateGroupingElement<E>(Expression<Func<T, E>> expression)
    {
        var body = new ProjectionInliningVisitor(expression.Parameters[0], Shape.Body).Visit(expression.Body)!;
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<Q, E>>(body, Shape.Parameters);
    }

    private Expression<Func<IGrouping<K, Q>, R>> TranslateGroup<K, R>(Expression<Func<IGrouping<K, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<K, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<K, Q, T>(groupQ, selector.Parameters[0], Shape).Visit(selector.Body);
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, groupQ);
    }

    private static Expression<Func<IGrouping<K, E>, R>> TranslateElementGroup<K, E, R>(Expression<Func<K, IEnumerable<E>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, E>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, E>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], group).Visit(body)!;
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, E>, R>>(body, group);
    }

    private Expression<Func<IGrouping<K, Q>, R>> TranslateInternalElementGroup<K, R>(Expression<Func<K, IEnumerable<T>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, Q>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, Q>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], ComposeEnumerable(Shape, group)).Visit(body)!;
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, group);
    }
    #endregion
}



internal sealed class GroupedQueryResult<K, KInternal, E, EInternal, T, Q>
    : DeferredQuery<IGrouping<K, E>, Pair<KInternal, EInternal[]>>
{
    private readonly IQueryable<Q> _source;
    private readonly Expression<Func<Q, K>> _key;
    private readonly Expression<Func<Q, EInternal>> _element;
    private readonly Expression<Func<EInternal, E>> _elementShape;

    internal GroupedQueryResult(
        IQueryable<Pair<KInternal, EInternal[]>> source,
        IQueryable<Q> originalSource,
        Expression<Func<Q, K>> key,
        Expression<Func<Q, EInternal>> element,
        Expression<Func<EInternal, E>> elementShape,
        Expression<Func<Pair<KInternal, EInternal[]>, IGrouping<K, E>>> shape) : base(source, shape)
    {
        _source = originalSource;
        _key = key;
        _element = element;
        _elementShape = elementShape;
    }

    public override IQuery<R> Select<R>(Expression<Func<IGrouping<K, E>, R>> mapping)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, EInternal>), mapping.Parameters[0].Name);
        var body = new GroupTranslateVisitor<K, EInternal, E>(group, mapping.Parameters[0], _elementShape)
            .Visit(mapping.Body);
        body = TupleExpressionNormalizer.Normalize(body!);

        var shape = Expression.Lambda<Func<IGrouping<K, EInternal>, R>>(body, group);

        return ProjectedGroupQueryBuilder<T, Q>.Create(_source, _key, _element, shape);
    }
}

internal static class RawGroupedQueryBuilder<T, Q>
{
    public static IQuery<IGrouping<K, E>> Create<K, E, EInternal>(
        IQueryable<Q> source,
        Expression<Func<Q, K>> key,
        Expression<Func<Q, EInternal>> element,
        Expression<Func<EInternal, E>> elementShape)
    {
        var loweredKey = TupleProjection<T, Q>.Lower(key.Body);
        var keyLambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(Q), loweredKey.Type),
            loweredKey,
            key.Parameters);

        return (IQuery<IGrouping<K, E>>)CreateRawGroupedQueryMethod
            .MakeGenericMethod(typeof(K), loweredKey.Type, typeof(E), typeof(EInternal))
            .Invoke(null, [source, key, keyLambda, element, elementShape])!;
    }

    private static GroupedQueryResult<K, KInternal, E, EInternal, T, Q> CreateRawGroupedQuery<K, KInternal, E, EInternal>(
        IQueryable<Q> source,
        Expression<Func<Q, K>> key,
        LambdaExpression loweredKey,
        Expression<Func<Q, EInternal>> element,
        Expression<Func<EInternal, E>> elementShape)
    {
        var group = Expression.Parameter(typeof(IGrouping<KInternal, EInternal>), "g");
        var projection = Expression.Lambda<Func<IGrouping<KInternal, EInternal>, Pair<KInternal, EInternal[]>>>(
            Expression.MemberInit(
                Expression.New(typeof(Pair<KInternal, EInternal[]>)),
                Expression.Bind(
                    typeof(Pair<KInternal, EInternal[]>).GetProperty(nameof(Pair<int, int>.Left))!,
                    Expression.Property(group, nameof(IGrouping<int, int>.Key))),
                Expression.Bind(
                    typeof(Pair<KInternal, EInternal[]>).GetProperty(nameof(Pair<int, int>.Right))!,
                    Expression.Call(EnumerableToArrayMethod.MakeGenericMethod(typeof(EInternal)), group))),
            group);

        var grouped = source
            .GroupBy((Expression<Func<Q, KInternal>>)loweredKey, element)
            .Select(projection);

        return new GroupedQueryResult<K, KInternal, E, EInternal, T, Q>(
            grouped,
            source,
            key,
            element,
            elementShape,
            CreateShape<K, KInternal, E, EInternal>(elementShape));
    }

    private static Expression<Func<Pair<KInternal, EInternal[]>, IGrouping<K, E>>> CreateShape<K, KInternal, E, EInternal>(
        Expression<Func<EInternal, E>> elementShape)
    {
        var pair = Expression.Parameter(typeof(Pair<KInternal, EInternal[]>), "p");
        var internalKey = Expression.Property(pair, nameof(Pair<int, int>.Left));
        Expression internalItems = Expression.Property(pair, nameof(Pair<int, int>.Right));

        var key = Expression.Parameter(typeof(KInternal), "k");
        var keyShape = Expression.Lambda<Func<KInternal, K>>(
            TupleProjection<T, Q>.Rebuild(key, typeof(K)),
            key);
        var holder = new GroupingShapeHolder<KInternal, K, EInternal, E>
        {
            KeyShape = keyShape.Compile(),
            ElementShape = elementShape.Compile()
        };
        var holderExpression = Expression.Constant(holder);

        var groupingType = typeof(ShapedGroupingValue<,,,>).MakeGenericType(
            typeof(KInternal),
            typeof(K),
            typeof(EInternal),
            typeof(E));

        var body = Expression.MemberInit(
            Expression.New(groupingType),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.InternalKey))!,
                internalKey),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.InternalItems))!,
                internalItems),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.KeyShape))!,
                Expression.Property(holderExpression, nameof(GroupingShapeHolder<int, int, int, int>.KeyShape))),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.ElementShape))!,
                Expression.Property(holderExpression, nameof(GroupingShapeHolder<int, int, int, int>.ElementShape))));

        return Expression.Lambda<Func<Pair<KInternal, EInternal[]>, IGrouping<K, E>>>(
            Expression.Convert(body, typeof(IGrouping<K, E>)),
            pair);
    }

    private static readonly MethodInfo EnumerableToArrayMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.ToArray) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters().Length == 1);

    private static readonly MethodInfo CreateRawGroupedQueryMethod =
        typeof(RawGroupedQueryBuilder<T, Q>).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(CreateRawGroupedQuery) && m.GetGenericArguments().Length == 4);
}
