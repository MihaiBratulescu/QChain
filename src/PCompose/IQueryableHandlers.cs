namespace PCompose;

using System.Linq.Expressions;

public static class IQueryableHandlers
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> Where(Expression<Func<T, Predicate>> predicate) =>
            query.Where(PredicateCompiler.Compile(predicate));

        public bool Any(Expression<Func<T, Predicate>> predicate) =>
            query.Any(PredicateCompiler.Compile(predicate));

        public bool All(Expression<Func<T, Predicate>> predicate) =>
            query.All(PredicateCompiler.Compile(predicate));

        public int Count(Expression<Func<T, Predicate>> predicate) =>
            query.Count(PredicateCompiler.Compile(predicate));

        public long LongCount(Expression<Func<T, Predicate>> predicate) =>
            query.LongCount(PredicateCompiler.Compile(predicate));

        public T First(Expression<Func<T, Predicate>> predicate) =>
            query.First(PredicateCompiler.Compile(predicate));

        public T? FirstOrDefault(Expression<Func<T, Predicate>> predicate) =>
            query.FirstOrDefault(PredicateCompiler.Compile(predicate));

        public T Last(Expression<Func<T, Predicate>> predicate) =>
            query.Last(PredicateCompiler.Compile(predicate));

        public T? LastOrDefault(Expression<Func<T, Predicate>> predicate) =>
            query.LastOrDefault(PredicateCompiler.Compile(predicate));

        public T Single(Expression<Func<T, Predicate>> predicate) =>
            query.Single(PredicateCompiler.Compile(predicate));

        public T? SingleOrDefault(Expression<Func<T, Predicate>> predicate) =>
            query.SingleOrDefault(PredicateCompiler.Compile(predicate));
    }
}
