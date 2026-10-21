using QChain.Internal.Types;

namespace QChain;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> AsQuery()
        {
            ArgumentNullException.ThrowIfNull(query);
            if (query is IShapedQuery)
                return query;

            var shape = new QueryShape<T>(query);
            var provider = new QueryProvider(query.Provider, shape);

            return new Query<T>(provider);
        }
    }
}
