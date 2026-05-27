using QChain.Internal.Visitors;
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

    public override IQueryShape Select<R>(Expression<Func<IGrouping<K, E>, R>> mapping)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, QG>), mapping.Parameters[0].Name);
        var body = new GroupTranslateVisitor<K, QG, E>(group, mapping.Parameters[0], elementShape)
            .Visit(mapping.Body);
        body = TupleExpressionNormalizer.Normalize(body!);

        var shape = Expression.Lambda<Func<IGrouping<K, QG>, R>>(body, group);

        return ((IUntypedQuery)ProjectedGroupQueryBuilder<T, Q>.Create(originalSource, key, element, shape)).Untyped;
    }
}
