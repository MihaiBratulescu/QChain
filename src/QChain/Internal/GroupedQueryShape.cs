using System.Linq.Expressions;

namespace QChain.Internal;

internal sealed class GroupedQueryShape<K, KQ, E, QG, T, Q>(
    IQueryable<Pair<KQ, QG[]>> source,
    IQueryable<Q> originalSource,
    Expression<Func<Q, K>> key,
    Expression<Func<Q, QG>> element,
    Expression<Func<QG, E>> elementShape,
    Expression<Func<Pair<KQ, QG[]>, IGrouping<K, E>>> shape)
    : SequenceQueryShape<IGrouping<K, E>, Pair<KQ, QG[]>>(source, shape)
{
    protected override SequenceQueryShape<IGrouping<K, E>, Pair<KQ, QG[]>> WithSource(IQueryable<Pair<KQ, QG[]>> source) =>
        new GroupedQueryShape<K, KQ, E, QG, T, Q>(source, originalSource, key, element, elementShape, Shape);

    public override IQueryShape Compose<R>(Expression<Func<IGrouping<K, E>, R>> outer) =>
        ((IUntypedQuery)GroupShapeBuilder<T, Q>.CreateProjected(originalSource, key, element, TranslateGroup(outer))).Untyped;

    private Expression<Func<IGrouping<K, QG>, R>> TranslateGroup<R>(Expression<Func<IGrouping<K, E>, R>> mapping)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, QG>), mapping.Parameters[0].Name);
        var body = new Internal.Visitors.GroupTranslateVisitor<K, QG, E>(group, mapping.Parameters[0], elementShape)
            .Visit(mapping.Body);
        body = Internal.Visitors.TupleExpressionNormalizer.Normalize(body!);

        return Expression.Lambda<Func<IGrouping<K, QG>, R>>(body, group);
    }
}
