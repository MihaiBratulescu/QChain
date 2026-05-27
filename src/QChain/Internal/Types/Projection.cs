namespace QChain.Internal;

internal readonly struct Projection<T1, T2>
{
    public required T1 Item1 { get; init; }
    public required T2 Item2 { get; init; }
}
