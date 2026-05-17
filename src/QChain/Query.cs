namespace QChain;

public class Query<T>(IQueryable<T> query) : DeferredQuery<T, T>(query, q => q)
{
}
