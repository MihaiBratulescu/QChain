namespace QChain.Internal;

internal sealed class DefaultIfEmptyValue<T>
{
    public bool HasValue { get; init; }
    public T Value { get; init; } = default!;
}