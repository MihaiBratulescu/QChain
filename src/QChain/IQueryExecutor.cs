namespace QChain;

public interface IQueryExecutor<T> : IAsyncQueryExecutor<T>, ISyncQueryExecutor<T>
{
    public string ToQueryString(IQuery<T> query);
}