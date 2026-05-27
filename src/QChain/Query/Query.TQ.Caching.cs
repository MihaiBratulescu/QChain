//using QChain.CachedQuery;
using QChain.Internal;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IUntypedQuery
{
    //public ICachedQuery<T> WithCaching(string key, TimeSpan expiry) =>
    //    new CachedQuery<T, Q>(this, key, expiry);
}
