using QChain.Internal;

namespace QChain;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQuery<T> AsQuery() => new DeferredQuery<T, T>(query, q => q);
    }
}
