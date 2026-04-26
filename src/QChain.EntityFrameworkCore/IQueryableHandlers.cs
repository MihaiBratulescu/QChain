namespace QChain.EntityFrameworkCore;

public static class IQueryableHandlers
{
    public static IQuery<T> AsQuery<T>(this IQueryable<T> query) =>
        new Query<T, T>(query, q => q);
}