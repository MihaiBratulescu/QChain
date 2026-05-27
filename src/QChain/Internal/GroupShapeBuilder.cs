using QChain.Internal.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal static class GroupShapeBuilder<T, Q>
{
    public static IQuery<IGrouping<K, T>> CreateRaw<K>(SequenceQueryShape<T, Q> query, Expression<Func<T, K>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return CreateRaw(query, query.Translate(selector), element, query.Shape);
    }

    public static IQuery<IGrouping<K, E>> CreateRaw<K, E>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> selector,
        Expression<Func<T, E>> elementSelector)
    {
        Expression<Func<E, E>> elementShape = e => e;

        return CreateRaw(
            query,
            query.Translate(selector),
            query.Translate(elementSelector),
            elementShape);
    }

    public static IQuery<R> CreateProjected<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> key,
        Expression<Func<IGrouping<K, T>, R>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            query.Source,
            query.Translate(key),
            element,
            TranslateGroup(query, selector));
    }

    public static IQuery<R> CreateProjected<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> key,
        Expression<Func<K, IEnumerable<T>, R>> resultsSelector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            query.Source,
            query.Translate(key),
            element,
            TranslateInternalElementGroup(query, resultsSelector));
    }

    public static IQuery<R> CreateProjected<K, E, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> key,
        Expression<Func<T, E>> elementSelector,
        Expression<Func<K, IEnumerable<E>, R>> resultsSelector)
    {
        return ProjectedGroupQueryBuilder<T, Q>.Create(
            query.Source,
            query.Translate(key),
            query.Translate(elementSelector),
            TranslateElementGroup(resultsSelector));
    }

    public static IQuery<R> CreateProjected<K, E, R>(
        IQueryable<Q> source,
        Expression<Func<Q, K>> key,
        Expression<Func<Q, E>> element,
        Expression<Func<IGrouping<K, E>, R>> shape) =>
        ProjectedGroupQueryBuilder<T, Q>.Create(source, key, element, shape);

    private static IQuery<IGrouping<K, E>> CreateRaw<K, E, QG>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<Q, K>> key,
        Expression<Func<Q, QG>> element,
        Expression<Func<QG, E>> elementShape)
    {
        var keyQ = TupleProjection<T, Q>.Lower(key.Body);
        var keyQLambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(Q), keyQ.Type),
            keyQ,
            key.Parameters);

        return (IQuery<IGrouping<K, E>>)CreateRawTypedMethod
            .MakeGenericMethod(typeof(K), keyQ.Type, typeof(E), typeof(QG))
            .Invoke(null, [query, key, keyQLambda, element, elementShape])!;
    }

    private static Query<IGrouping<K, E>, IGrouping<KQ, QG>> CreateRawTyped<K, KQ, E, QG>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<Q, K>> key,
        LambdaExpression keyQ,
        Expression<Func<Q, QG>> element,
        Expression<Func<QG, E>> elementShape)
    {
        var shape = new GroupedQueryShape<K, KQ, E, QG, T, Q>(
            query.Source.GroupBy((Expression<Func<Q, KQ>>)keyQ, element),
            query.Source,
            key,
            keyQ,
            element,
            elementShape,
            CreateRawGroupShape<K, KQ, E, QG>(elementShape));

        return new Query<IGrouping<K, E>, IGrouping<KQ, QG>>(shape);
    }

    private static Expression<Func<IGrouping<KQ, QG>, IGrouping<K, E>>> CreateRawGroupShape<K, KQ, E, QG>(
        Expression<Func<QG, E>> elementShape)
    {
        var group = Expression.Parameter(typeof(IGrouping<KQ, QG>), "g");
        var keyQ = Expression.Property(group, nameof(IGrouping<int, int>.Key));
        var itemsQ = Expression.Call(EnumerableToArrayMethod.MakeGenericMethod(typeof(QG)), group);

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

        return Expression.Lambda<Func<IGrouping<KQ, QG>, IGrouping<K, E>>>(
            Expression.Convert(body, typeof(IGrouping<K, E>)),
            group);
    }

    private static Expression<Func<IGrouping<K, Q>, R>> TranslateGroup<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<IGrouping<K, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<K, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<K, Q, T>(groupQ, selector.Parameters[0], query.Shape).Visit(selector.Body);
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, groupQ);
    }

    private static Expression<Func<IGrouping<K, E>, R>> TranslateElementGroup<K, E, R>(
        Expression<Func<K, IEnumerable<E>, R>> selector)
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

    private static Expression<Func<IGrouping<K, Q>, R>> TranslateInternalElementGroup<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<K, IEnumerable<T>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, Q>), "g");

        var body = ReplaceExpressionVisitor.ReplaceMany(selector.Body, new Dictionary<Expression, Expression>
        {
            [selector.Parameters[0]] = Expression.Property(group, nameof(IGrouping<K, Q>.Key)),
            [selector.Parameters[1]] = ComposeEnumerable(query.Shape, group)
        });
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, group);
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

    private static readonly MethodInfo EnumerableToArrayMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.ToArray) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters().Length == 1);

    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));

    private static readonly MethodInfo CreateRawTypedMethod =
        typeof(GroupShapeBuilder<T, Q>).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(CreateRawTyped) && m.GetGenericArguments().Length == 4);
}
