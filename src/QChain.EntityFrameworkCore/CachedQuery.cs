namespace QChain.EntityFrameworkCore;

internal class CachedQuery<T, Q> : Query<T, Q>, ICachedQuery<T>
{
    public string Key { get; }
    public TimeSpan Expiry { get; }

    public CachedQuery(Query<T, Q> query, string key, TimeSpan expiry) : base(query)
    {
        Key = key;
        Expiry = expiry;
    }
}
