using QChain.CachedQuery;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    //public ICachedQuery<T> WithCaching(string key, TimeSpan expiry) =>
    //    new CachedQuery<T, Q>(this, key, expiry);
}
