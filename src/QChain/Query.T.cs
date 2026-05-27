namespace QChain;

public class Query<T>(IQueryable<T> query) : Query<T, T>(query, q => q)
{
}
