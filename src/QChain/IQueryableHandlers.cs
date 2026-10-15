namespace QChain;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> AsQuery()
        {
            var shape = new QueryShape<T>(query);
            var provider = new QueryProvider(query.Provider, shape);

            return new Query<T>(provider, query.Expression);
        }
    }
}
