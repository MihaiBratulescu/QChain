using QChain;

namespace QChain;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQuery<T> AsQuery() => new Query<T, T>(query, q => q);
    }
}
