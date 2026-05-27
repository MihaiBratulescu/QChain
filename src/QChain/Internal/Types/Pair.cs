namespace QChain.Internal;

internal readonly struct Pair<T1, T2>
{
    public required T1 Left { get; init; }
    public required T2 Right { get; init; }
}
