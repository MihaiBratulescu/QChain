namespace PCompose;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> Where(Func<T, Predicate> predicate) =>
            query.Where(predicate.Compile());

        public bool Any(Func<T, Predicate> predicate) =>
            query.Any(predicate.Compile());

        public bool All(Func<T, Predicate> predicate) =>
            query.All(predicate.Compile());

        public int Count(Func<T, Predicate> predicate) =>
            query.Count(predicate.Compile());

        public long LongCount(Func<T, Predicate> predicate) =>
            query.LongCount(predicate.Compile());

        public T First(Func<T, Predicate> predicate) =>
            query.First(predicate.Compile());

        public T? FirstOrDefault(Func<T, Predicate> predicate) =>
            query.FirstOrDefault(predicate.Compile());

        public T Last(Func<T, Predicate> predicate) =>
            query.Last(predicate.Compile());

        public T? LastOrDefault(Func<T, Predicate> predicate) =>
            query.LastOrDefault(predicate.Compile());

        public T Single(Func<T, Predicate> predicate) =>
            query.Single(predicate.Compile());

        public T? SingleOrDefault(Func<T, Predicate> predicate) =>
            query.SingleOrDefault(predicate.Compile());
    }
}
