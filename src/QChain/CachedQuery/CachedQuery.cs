using QChain.Internal;

namespace QChain.CachedQuery;

internal sealed class CachedQuery<T, Q> : DeferredQuery<T, Q>, ICachedQuery<T>
{
    public string Key { get; }
    public TimeSpan Expiry { get; }

    public CachedQuery(DeferredQuery<T, Q> query, string key, TimeSpan expiry) : base(query)
    {
        Key = key;
        Expiry = expiry;
    }
}

