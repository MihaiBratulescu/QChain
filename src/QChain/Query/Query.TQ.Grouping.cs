using QChain.Internal;
using QChain.Visitors;

using System.Linq.Expressions;
using System.Reflection;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    //IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>
    public IQuery<IGrouping<K, T>> GroupBy<K>(Expression<Func<T, K>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return CreateRawGroup(Translate(selector), element, Shape);
    }

    //IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>
    public IQuery<IGrouping<K, E>> GroupBy<K, E>(Expression<Func<T, K>> selector, Expression<Func<T, E>> elementSelector)
    {
        Expression<Func<E, E>> elementShape = e => e;

        return CreateRawGroup(
            Translate(selector),
            Translate(elementSelector),
            elementShape);
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

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = Expression.Property(group, nameof(IGrouping<K, E>.Key)),
            [selector.Parameters[1]] = group
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, E>, R>>(body, group);
    }

    private Expression<Func<IGrouping<K, Q>, R>> TranslateInternalElementGroup<K, R>(Expression<Func<K, IEnumerable<T>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, Q>), "g");

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = Expression.Property(group, nameof(IGrouping<K, Q>.Key)),
            [selector.Parameters[1]] = ComposeEnumerable(Shape, group)
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, group);
    }

    private IQuery<IGrouping<K, E>> CreateRawGroup<K, E, QG>(
        Expression<Func<Q, K>> key,
        Expression<Func<Q, QG>> element,
        Expression<Func<QG, E>> elementShape)
    {
        var keyQ = TupleProjection<T, Q>.Lower(key.Body);
        var keyQLambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(Q), keyQ.Type),
            keyQ,
            key.Parameters);

        return (IQuery<IGrouping<K, E>>)CreateRawGroupTypedMethod
            .MakeGenericMethod(typeof(K), keyQ.Type, typeof(E), typeof(QG))
            .Invoke(this, [key, keyQLambda, element, elementShape])!;
    }

    private GroupedQueryResult<K, KQ, E, QG, T, Q> CreateRawGroupTyped<K, KQ, E, QG>(
        Expression<Func<Q, K>> key,
        LambdaExpression keyQ,
        Expression<Func<Q, QG>> element,
        Expression<Func<QG, E>> elementShape)
    {
        var group = Expression.Parameter(typeof(IGrouping<KQ, QG>), "g");
        var pair = Expression.Lambda<Func<IGrouping<KQ, QG>, Pair<KQ, QG[]>>>(
            Expression.MemberInit(
                Expression.New(typeof(Pair<KQ, QG[]>)),
                Expression.Bind(
                    typeof(Pair<KQ, QG[]>).GetProperty(nameof(Pair<int, int>.Left))!,
                    Expression.Property(group, nameof(IGrouping<int, int>.Key))),
                Expression.Bind(
                    typeof(Pair<KQ, QG[]>).GetProperty(nameof(Pair<int, int>.Right))!,
                    Expression.Call(EnumerableToArrayMethod.MakeGenericMethod(typeof(QG)), group))),
            group);
        var translateShape = CreateRawGroupShape<K, KQ, E, QG>(elementShape);

        return new GroupedQueryResult<K, KQ, E, QG, T, Q>(
            Source.GroupBy((Expression<Func<Q, KQ>>)keyQ, element).Select(pair),
            Source,
            key,
            element,
            elementShape,
            translateShape);
    }

    private static Expression<Func<Pair<KQ, QG[]>, IGrouping<K, E>>> CreateRawGroupShape<K, KQ, E, QG>(
        Expression<Func<QG, E>> elementShape)
    {
        var pair = Expression.Parameter(typeof(Pair<KQ, QG[]>), "p");
        var keyQ = Expression.Property(pair, nameof(Pair<int, int>.Left));
        var itemsQ = Expression.Property(pair, nameof(Pair<int, int>.Right));

        var key = Expression.Parameter(typeof(KQ), "k");
        var keyShape = Expression.Lambda<Func<KQ, K>>(
            TupleProjection<T, Q>.Rebuild(key, typeof(K)),
            key);
        var holder = new GroupingShapeHolder<KQ, K, QG, E>
        {
            KeyShape = keyShape.Compile(),
            ElementShape = elementShape.Compile()
        };
        var holderExpression = Expression.Constant(holder);

        var groupingType = typeof(ShapedGroupingValue<,,,>).MakeGenericType(
            typeof(KQ),
            typeof(K),
            typeof(QG),
            typeof(E));

        var body = Expression.MemberInit(
            Expression.New(groupingType),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.InternalKey))!,
                keyQ),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.InternalItems))!,
                itemsQ),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.KeyShape))!,
                Expression.Property(holderExpression, nameof(GroupingShapeHolder<int, int, int, int>.KeyShape))),
            Expression.Bind(
                groupingType.GetProperty(nameof(ShapedGroupingValue<int, int, int, int>.ElementShape))!,
                Expression.Property(holderExpression, nameof(GroupingShapeHolder<int, int, int, int>.ElementShape))));

        return Expression.Lambda<Func<Pair<KQ, QG[]>, IGrouping<K, E>>>(
            Expression.Convert(body, typeof(IGrouping<K, E>)),
            pair);
    }

    private static readonly MethodInfo EnumerableToArrayMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.ToArray) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters().Length == 1);

    private static readonly MethodInfo CreateRawGroupTypedMethod =
        typeof(Query<T, Q>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == nameof(CreateRawGroupTyped) && m.GetGenericArguments().Length == 4);
    #endregion
}

internal sealed class GroupedQueryResult<K, KQ, E, QG, T, Q>
    : Query<IGrouping<K, E>, Pair<KQ, QG[]>>
{
    private readonly IQueryable<Q> _source;
    private readonly Expression<Func<Q, K>> _key;
    private readonly Expression<Func<Q, QG>> _element;
    private readonly Expression<Func<QG, E>> _elementShape;

    internal GroupedQueryResult(
        IQueryable<Pair<KQ, QG[]>> source,
        IQueryable<Q> originalSource,
        Expression<Func<Q, K>> key,
        Expression<Func<Q, QG>> element,
        Expression<Func<QG, E>> elementShape,
        Expression<Func<Pair<KQ, QG[]>, IGrouping<K, E>>> shape) : base(source, shape)
    {
        _source = originalSource;
        _key = key;
        _element = element;
        _elementShape = elementShape;
    }

    public override IQuery<R> Select<R>(Expression<Func<IGrouping<K, E>, R>> mapping)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, QG>), mapping.Parameters[0].Name);
        var body = new GroupTranslateVisitor<K, QG, E>(group, mapping.Parameters[0], _elementShape)
            .Visit(mapping.Body);
        body = TupleExpressionNormalizer.Normalize(body!);

        var shape = Expression.Lambda<Func<IGrouping<K, QG>, R>>(body, group);

        return ProjectedGroupQueryBuilder<T, Q>.Create(_source, _key, _element, shape);
    }
}
