namespace QChain;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> AsQuery() => 
            new Query<T>(new QueryProvider(query.Provider), query.Expression);
    }
}
