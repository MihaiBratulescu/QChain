using System.Collections;
using System.Linq.Expressions;

namespace QChain;

internal interface IShapedQuery
{
    IQueryShape QueryShape { get; }
}

internal sealed class Query<T>(QueryProvider provider) : IOrderedQueryable<T>, IAsyncEnumerable<T>, IShapedQuery
{
    public Type ElementType => typeof(T);
    public Expression Expression => Expression.Constant(this);
    public IQueryProvider Provider => provider;
    IQueryShape IShapedQuery.QueryShape => provider.QueryShape;

    public IEnumerator<T> GetEnumerator() => ((IQueryable<T>)provider.QueryShape.Project()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (provider.QueryShape.Project() is IAsyncEnumerable<T> asyncQuery)
            return asyncQuery.GetAsyncEnumerator(cancellationToken);

        throw new InvalidOperationException("The underlying query does not support asynchronous enumeration.");
    }
}
