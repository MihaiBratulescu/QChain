using System.Collections;

namespace QChain.Internal;

internal sealed class ShapedGroupingValue<KInternal, K, EInternal, E> : IGrouping<K, E>
{
    public required KInternal InternalKey { get; init; }
    public required IEnumerable<EInternal> InternalItems { get; init; }
    public required Func<KInternal, K> KeyShape { get; init; }
    public required Func<EInternal, E> ElementShape { get; init; }

    public K Key => KeyShape(InternalKey);

    public IEnumerator<E> GetEnumerator() => InternalItems.Select(ElementShape).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
