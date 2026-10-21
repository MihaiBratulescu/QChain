namespace QChain.Internal;

// The default row has a false marker, including when the source runs in memory.
internal readonly struct DefaultIfEmptyValue<T>
{
    public bool HasValue { get; init; }
    public T Value { get; init; }
}
