namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Skip(int count) => new DeferredQuery<T, Q>(Source.Skip(count), Shape);
    public IQuery<T> Take(int count) => new DeferredQuery<T, Q>(Source.Take(count), Shape);
    public IQuery<T> Page(int index, int count) => new DeferredQuery<T, Q>(Source.Skip(index * count).Take(count), Shape);
}
