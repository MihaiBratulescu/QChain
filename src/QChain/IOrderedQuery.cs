using System.Linq.Expressions;

namespace QChain;

public interface IOrderedQuery<T> : IQuery<T>
{
    IOrderedQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> selector);
    IOrderedQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> selector);
}
