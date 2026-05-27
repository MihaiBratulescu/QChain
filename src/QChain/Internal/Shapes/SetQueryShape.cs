using QChain.Internal.Shapes;
using System.Linq.Expressions;

namespace QChain.Internal;

internal sealed class SetQueryShape<T, QCarrier>(
    IQueryable<QCarrier> source,
    Expression<Func<QCarrier, T>> shape)
    : SequenceQueryShape<T, QCarrier>(source, shape)
{
    protected override SequenceQueryShape<T, QCarrier> WithSource(IQueryable<QCarrier> source) =>
        new SetQueryShape<T, QCarrier>(source, Shape);
}
