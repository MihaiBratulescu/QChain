using QChain.Internal.Grouping;
using System.Linq.Expressions;

namespace QChain.Internal;

internal sealed class GroupedQueryShape<K, KQ, E, QG, T, Q>
    : SequenceQueryShape<IGrouping<K, E>, IGrouping<KQ, QG>>
{
    private readonly Expression<Func<QG, E>> _elementShape;

    internal GroupedQueryShape(
        IQueryable<IGrouping<KQ, QG>> source,
        Expression<Func<QG, E>> elementShape,
        Expression<Func<IGrouping<KQ, QG>, IGrouping<K, E>>> shape) : base(source, shape)
    {
        _elementShape = elementShape;
    }

    protected override SequenceQueryShape<IGrouping<K, E>, IGrouping<KQ, QG>> WithSource(IQueryable<IGrouping<KQ, QG>> source) =>
        new GroupedQueryShape<K, KQ, E, QG, T, Q>(source, _elementShape, Shape);

    public override Expression<Func<IGrouping<KQ, QG>, R>> Translate<R>(Expression<Func<IGrouping<K, E>, R>> expression) =>
        GroupedShapeProjectionBuilder<K, KQ, E, QG, T, Q>.Translate(expression, _elementShape);

    public override IQueryShape Compose<R>(Expression<Func<IGrouping<K, E>, R>> outer) =>
        GroupedShapeProjectionBuilder<K, KQ, E, QG, T, Q>.Compose(Source, outer, _elementShape);
}
