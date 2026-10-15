using System.Collections;
using System.Linq.Expressions;

namespace QChain;

internal sealed class Query<T>(IQueryProvider provider, Expression expression) : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    public Type ElementType => typeof(T);
    public Expression Expression { get; } = expression;
    public IQueryProvider Provider { get; } = provider;

    public IEnumerator<T> GetEnumerator() => Provider
        .Execute<IEnumerable<T>>(Expression)
        .GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); 

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var query = Provider.CreateQuery<T>(Expression);

        if (query is IAsyncEnumerable<T> asyncQuery)
            return asyncQuery.GetAsyncEnumerator(cancellationToken);

        throw new InvalidOperationException("The underlying query does not support asynchronous enumeration.");
    }
}
