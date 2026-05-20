using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace QChain;

public static class EntityFrameworkExtensions
{
    extension<T>(IQuery<T> query) where T : class
    {
        #region Tracking
        public IQuery<T> AsNoTracking() =>
            new Query<T>(query.AsQueryable().AsNoTracking());

        public IQuery<T> AsNoTrackingWithIdentityResolution() =>
            new Query<T>(query.AsQueryable().AsNoTrackingWithIdentityResolution());

        public IQuery<T> AsTracking() =>
            new Query<T>(query.AsQueryable().AsTracking());
        #endregion

        #region Single/Split
        public IQuery<T> AsSingleQuery() =>
            new Query<T>(query.AsQueryable().AsSingleQuery());

        public IQuery<T> AsSplitQuery() =>
            new Query<T>(query.AsQueryable().AsSplitQuery());
        #endregion

        public IQuery<T> Include<E>(Expression<Func<T, E>> include) =>
            new Query<T>(query.AsQueryable().Include(include));
    }

    extension<T>(IQuery<T> query)
    {
        public string ToQueryString() =>
            query.AsQueryable().ToQueryString();
    }
}
