namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T?> DefaultIfEmpty() =>
        new Query<T?>(Source.Select(Shape).DefaultIfEmpty());
}
