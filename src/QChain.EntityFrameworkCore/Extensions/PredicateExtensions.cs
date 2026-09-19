using PCompose;

namespace QChain;

using System.Linq.Expressions;

public static class PredicateExtensions
{
    extension<T>(IQuery<T> query)
    {
        #region Async
        public Task<bool> AnyAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).AnyAsync(ct);
        public Task<bool> AllAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            query.AllAsync(PredicateCompiler.Compile(predicate), ct);

        public Task<int> CountAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
           Query(query, predicate).CountAsync(ct);

        public Task<long> LongCountAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).LongCountAsync(ct);

        public Task<T> FirstAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).FirstAsync(ct);

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).FirstOrDefaultAsync(ct);

        public Task<T> LastAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).LastAsync(ct);

        public Task<T?> LastOrDefaultAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).LastOrDefaultAsync(ct);

        public Task<T> SingleAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).SingleAsync(ct);

        public Task<T?> SingleOrDefaultAsync(Expression<Func<T, Predicate>> predicate, CancellationToken ct = default) =>
            Query(query, predicate).SingleOrDefaultAsync(ct);
        #endregion

        #region Sync
        public bool Any(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).Any();
        public bool All(Expression<Func<T, Predicate>> predicate) =>
            query.All(PredicateCompiler.Compile(predicate));

        public int Count(Expression<Func<T, Predicate>> predicate) =>
           Query(query, predicate).Count();

        public long LongCount(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).LongCount();

        public T First(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).First();

        public T? FirstOrDefault(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).FirstOrDefault();

        public T Last(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).Last();

        public T? LastOrDefault(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).LastOrDefault();

        public T Single(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).Single();

        public T? SingleOrDefault(Expression<Func<T, Predicate>> predicate) =>
            Query(query, predicate).SingleOrDefault();
        #endregion

        private IQuery<T> Query(Expression<Func<T, Predicate>> predicate) => query.Where(predicate);
    }
}
