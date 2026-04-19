namespace QChain;

public interface ICachedQuery<T> : IQuery<T>
{
    public string Key { get; }
    public TimeSpan Expiry { get; }
}
