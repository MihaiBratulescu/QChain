using QChain.Internal.Shapes;
using System.Linq.Expressions;

namespace QChain.Internal;

internal sealed class ProjectedQueryShape<T, Q>(
    IQueryable<Q> source,
    Expression<Func<Q, T>> shape)
    : SequenceQueryShape<T, Q>(source, shape)
{
    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new ProjectedQueryShape<T, Q>(source, Shape);
}
