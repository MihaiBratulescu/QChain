namespace QChain.Internal;

// Preserve value-tuple presence when a nullable tuple or an empty sequence has no value.
internal readonly struct DefaultIfEmptyValue<T>
{
    private readonly T value;

    public bool HasValue { get; init; }
    public T Value
    {
        get => HasValue ? value : throw new InvalidOperationException("Nullable object must have a value.");
        init => this.value = value;
    }
}

// EF-facing row shape: all values can be null when a left join finds no row.
internal sealed class DefaultIfEmptyCarrier<T>
{
    public bool HasValue { get; init; }
    public T Value { get; init; } = default!;
}
