namespace QChain.Internal;

// Preserve value-tuple presence when a nullable tuple or an empty sequence has no value.
internal readonly struct DefaultIfEmptyValue<T>
{
    public bool HasValue { get; init; }
    public T Value { get; init; }
}
