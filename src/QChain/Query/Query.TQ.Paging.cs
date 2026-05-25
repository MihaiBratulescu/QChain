using QChain.Internal;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Skip(int count) => new Query<T, Q>(Source.Skip(count), Shape);
    public IQuery<T> Take(int count) => new Query<T, Q>(Source.Take(count), Shape);
    public IQuery<T> Page(int index, int count) => new Query<T, Q>(Source.Skip(index * count).Take(count), Shape);
}
