namespace PCompose;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> Where(Func<T, Predicate> predicate) =>
            Queryable.Where(query, PredicateCompiler.Compile(predicate));

        public bool Any(Func<T, Predicate> predicate) =>
            Queryable.Any(query, PredicateCompiler.Compile(predicate));

        public bool All(Func<T, Predicate> predicate) =>
            Queryable.All(query, PredicateCompiler.Compile(predicate));

        public int Count(Func<T, Predicate> predicate) =>
            Queryable.Count(query, PredicateCompiler.Compile(predicate));

        public long LongCount(Func<T, Predicate> predicate) =>
            Queryable.LongCount(query, PredicateCompiler.Compile(predicate));

        public T First(Func<T, Predicate> predicate) =>
            Queryable.First(query, PredicateCompiler.Compile(predicate));

        public T? FirstOrDefault(Func<T, Predicate> predicate) =>
            Queryable.FirstOrDefault(query, PredicateCompiler.Compile(predicate));

        public T Last(Func<T, Predicate> predicate) =>
            Queryable.Last(query, PredicateCompiler.Compile(predicate));

        public T? LastOrDefault(Func<T, Predicate> predicate) =>
            Queryable.LastOrDefault(query, PredicateCompiler.Compile(predicate));

        public T Single(Func<T, Predicate> predicate) =>
            Queryable.Single(query, PredicateCompiler.Compile(predicate));

        public T? SingleOrDefault(Func<T, Predicate> predicate) =>
            Queryable.SingleOrDefault(query, PredicateCompiler.Compile(predicate));
    }
}
