using System.Linq.Expressions;

namespace QChain.Internal;

internal sealed class RegularQueryShape<T, Q>(
    IQueryable<Q> source,
    Expression<Func<Q, T>> shape) : SequenceQueryShape<T, Q>(source, shape)
{
    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new RegularQueryShape<T, Q>(source, Shape);
}

internal sealed class ProjectedQueryShape<T, Q>(
    IQueryable<Q> source,
    Expression<Func<Q, T>> shape) : SequenceQueryShape<T, Q>(source, shape)
{
    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new ProjectedQueryShape<T, Q>(source, Shape);
}

internal sealed class JoinedQueryShape<T, Q>(
    IQueryable<Q> source,
    Expression<Func<Q, T>> shape) : SequenceQueryShape<T, Q>(source, shape)
{
    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new JoinedQueryShape<T, Q>(source, Shape);
}

internal sealed class SetQueryShape<T, Q>(
    IQueryable<Q> source,
    Expression<Func<Q, T>> shape) : SequenceQueryShape<T, Q>(source, shape)
{
    protected override SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source) =>
        new SetQueryShape<T, Q>(source, Shape);
}
