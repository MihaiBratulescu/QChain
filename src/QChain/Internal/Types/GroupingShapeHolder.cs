namespace QChain.Internal;

internal sealed class GroupingShapeHolder<KInternal, K, EInternal, E>
{
    public required Func<KInternal, K> KeyShape { get; init; }
    public required Func<EInternal, E> ElementShape { get; init; }
}
