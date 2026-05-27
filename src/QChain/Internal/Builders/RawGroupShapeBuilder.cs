using QChain.Internal.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Builders;

internal static class RawGroupShapeBuilder<T, Q>
{
    public static IQuery<IGrouping<K, T>> Create<K>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return Create(query, query.Translate(selector), element, query.Shape);
    }

    public static IQuery<IGrouping<K, E>> Create<K, E>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> selector,
        Expression<Func<T, E>> elementSelector)
    {
        Expression<Func<E, E>> elementShape = e => e;

        return Create(
            query,
            query.Translate(selector),
            query.Translate(elementSelector),
            elementShape);
    }

    private static IQuery<IGrouping<K, E>> Create<K, E, QG>(
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

        return (IQuery<IGrouping<K, E>>)CreateTypedMethod
            .MakeGenericMethod(typeof(K), keyQ.Type, typeof(E), typeof(QG))
            .Invoke(null, [query, key, keyQLambda, element, elementShape])!;
    }

    private static Query<IGrouping<K, E>, IGrouping<KQ, QG>> CreateTyped<K, KQ, E, QG>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<Q, K>> key,
        LambdaExpression keyQ,
        Expression<Func<Q, QG>> element,
        Expression<Func<QG, E>> elementShape)
    {
        var shape = new GroupedQueryShape<K, KQ, E, QG, T, Q>(
            query.Source.GroupBy((Expression<Func<Q, KQ>>)keyQ, element),
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

    private static readonly MethodInfo EnumerableToArrayMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.ToArray) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters().Length == 1);

    private static readonly MethodInfo CreateTypedMethod =
        typeof(RawGroupShapeBuilder<T, Q>).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(CreateTyped) && m.GetGenericArguments().Length == 4);
}
