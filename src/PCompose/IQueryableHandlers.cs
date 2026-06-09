namespace PCompose;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> Where(Func<T, Predicate> predicate) =>
            Queryable.Where(query, predicate.Compile());

        public bool Any(Func<T, Predicate> predicate) =>
            Queryable.Any(query, predicate.Compile());

        public bool All(Func<T, Predicate> predicate) =>
            Queryable.All(query, predicate.Compile());

        public int Count(Func<T, Predicate> predicate) =>
            Queryable.Count(query, predicate.Compile());

        public long LongCount(Func<T, Predicate> predicate) =>
            Queryable.LongCount(query, predicate.Compile());

        public T First(Func<T, Predicate> predicate) =>
            Queryable.First(query, predicate.Compile());

        public T? FirstOrDefault(Func<T, Predicate> predicate) =>
            Queryable.FirstOrDefault(query, predicate.Compile());

        public T Last(Func<T, Predicate> predicate) =>
            Queryable.Last(query, predicate.Compile());

        public T? LastOrDefault(Func<T, Predicate> predicate) =>
            Queryable.LastOrDefault(query, predicate.Compile());

        public T Single(Func<T, Predicate> predicate) =>
            Queryable.Single(query, predicate.Compile());

        public T? SingleOrDefault(Func<T, Predicate> predicate) =>
            Queryable.SingleOrDefault(query, predicate.Compile());
    }
}
