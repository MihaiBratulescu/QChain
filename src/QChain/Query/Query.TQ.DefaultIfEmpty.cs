using QChain.Internal;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> DefaultIfEmpty() =>
        new Query<T>(Source.Select(Shape).DefaultIfEmpty());
}
