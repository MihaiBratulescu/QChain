using PCompose;

namespace QChain;

public static class PredicateExtensions
{
    extension<T>(IQuery<T> query)
    {
        #region Async
        public Task<bool> AnyAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).AnyAsync(ct);
        public Task<bool> AllAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            query.AllAsync(predicate.Compile(), ct);

        public Task<int> CountAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
           Query(query, predicate).CountAsync(ct);

        public Task<long> LongCountAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).LongCountAsync(ct);

        public Task<T> FirstAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).FirstAsync(ct);

        public Task<T?> FirstOrDefaultAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).FirstOrDefaultAsync(ct);

        public Task<T> LastAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).LastAsync(ct);

        public Task<T?> LastOrDefaultAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).LastOrDefaultAsync(ct);

        public Task<T> SingleAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).SingleAsync(ct);

        public Task<T?> SingleOrDefaultAsync(Func<T, Predicate> predicate, CancellationToken ct = default) =>
            Query(query, predicate).SingleOrDefaultAsync(ct);
        #endregion

        #region Sync
        public bool Any(Func<T, Predicate> predicate) =>
            Query(query, predicate).Any();
        public bool All(Func<T, Predicate> predicate) =>
            query.All(predicate.Compile());

        public int Count(Func<T, Predicate> predicate) =>
           Query(query, predicate).Count();

        public long LongCount(Func<T, Predicate> predicate) =>
            Query(query, predicate).LongCount();

        public T First(Func<T, Predicate> predicate) =>
            Query(query, predicate).First();

        public T? FirstOrDefault(Func<T, Predicate> predicate) =>
            Query(query, predicate).FirstOrDefault();

        public T Last(Func<T, Predicate> predicate) =>
            Query(query, predicate).Last();

        public T? LastOrDefault(Func<T, Predicate> predicate) =>
            Query(query, predicate).LastOrDefault();

        public T Single(Func<T, Predicate> predicate) =>
            Query(query, predicate).Single();

        public T? SingleOrDefault(Func<T, Predicate> predicate) =>
            Query(query, predicate).SingleOrDefault();
        #endregion

        private IQuery<T> Query(Func<T, Predicate> predicate) => query.Where(predicate);
    }
}
