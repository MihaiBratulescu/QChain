using System.Collections;

namespace QChain.Internal.Types;

internal sealed class ProjectedGrouping<K, T>(K key, IEnumerable<T> items) : IGrouping<K, T>
{
    public K Key => key;
    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
