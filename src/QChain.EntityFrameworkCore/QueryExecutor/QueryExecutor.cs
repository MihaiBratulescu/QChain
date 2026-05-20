namespace QChain.EntityFrameworkCore;


public partial class QueryExecutor<T>(IQuery<T> query) : IQueryExecutor<T>
{
    public string ToQueryString(IQuery<T> query)
        => query.ToQueryString();
}
