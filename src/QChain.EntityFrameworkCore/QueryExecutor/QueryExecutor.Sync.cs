using PCompose;
using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

public partial class QueryExecutor<T> : IQueryExecutor<T>
{
    public bool Any() => query.Any();

    public bool Any(Expression<Func<T, bool>> predicate) => query.Any(predicate);

    public bool Any(Func<T, Predicate> predicate) => Any(predicate.Compile());

    public bool All(Expression<Func<T, bool>> predicate) => query.All(predicate);

    public bool All(Func<T, Predicate> predicate) => All(predicate.Compile());

    public int Count() => query.Count();

    public int Count(Expression<Func<T, bool>> predicate)
        => query.Count(predicate);

    public int Count(Func<T, Predicate> predicate)
        => Count(predicate.Compile());

    public long LongCount() => query.LongCount();

    public long LongCount(Expression<Func<T, bool>> predicate)
        => query.LongCount(predicate);

    public long LongCount(Func<T, Predicate> predicate)
        => LongCount(predicate.Compile());

    public T ElementAt(int index)
        => query.ElementAt(index);

    public T? ElementAtOrDefault(int index)
        => query.ElementAtOrDefault(index);

    public bool Contains(T item)
        => query.Contains(item);

    public T First() => query.First();

    public T First(Expression<Func<T, bool>> predicate)
        => query.First(predicate);

    public T First(Func<T, Predicate> predicate)
        => First(predicate.Compile());

    public T? FirstOrDefault() => query.FirstOrDefault();

    public T? FirstOrDefault(Expression<Func<T, bool>> predicate)
        => query.FirstOrDefault(predicate);

    public T? FirstOrDefault(Func<T, Predicate> predicate)
        => FirstOrDefault(predicate.Compile());

    public T Last() => query.Last();

    public T Last(Expression<Func<T, bool>> predicate)
        => query.Last(predicate);

    public T Last(Func<T, Predicate> predicate)
        => Last(predicate.Compile());

    public T? LastOrDefault() => query.LastOrDefault();

    public T? LastOrDefault(Expression<Func<T, bool>> predicate)
        => query.LastOrDefault(predicate);

    public T? LastOrDefault(Func<T, Predicate> predicate)
        => LastOrDefault(predicate.Compile());

    public T Single() => query.Single();

    public T Single(Expression<Func<T, bool>> predicate)
        => query.Single(predicate);

    public T Single(Func<T, Predicate> predicate)
        => Single(predicate.Compile());

    public T? SingleOrDefault()
        => query.SingleOrDefault();

    public T? SingleOrDefault(Expression<Func<T, bool>> predicate)
        => query.SingleOrDefault(predicate);

    public T? SingleOrDefault(Func<T, Predicate> predicate)
        => SingleOrDefault(predicate.Compile());

    public T? Min() => query.Min();

    public R? Min<R>(Expression<Func<T, R>> selector)
        => query.Min(selector);

    public T? Max() => query.Max();

    public R? Max<R>(Expression<Func<T, R>> selector)
        => query.Max(selector);

    public decimal Sum(Expression<Func<T, decimal>> selector)
        => query.Sum(selector);

    public decimal? Sum(Expression<Func<T, decimal?>> selector)
        => query.Sum(selector);

    public int Sum(Expression<Func<T, int>> selector)
        => query.Sum(selector);

    public int? Sum(Expression<Func<T, int?>> selector)
        => query.Sum(selector);

    public long Sum(Expression<Func<T, long>> selector)
        => query.Sum(selector);

    public long? Sum(Expression<Func<T, long?>> selector)
        => query.Sum(selector);

    public float Sum(Expression<Func<T, float>> selector)
        => query.Sum(selector);

    public float? Sum(Expression<Func<T, float?>> selector)
        => query.Sum(selector);

    public double Sum(Expression<Func<T, double>> selector)
        => query.Sum(selector);

    public double? Sum(Expression<Func<T, double?>> selector)
        => query.Sum(selector);

    public decimal Average(Expression<Func<T, decimal>> selector)
        => query.Average(selector);

    public decimal? Average(Expression<Func<T, decimal?>> selector)
        => query.Average(selector);

    public double Average(Expression<Func<T, int>> selector)
        => query.Average(selector);

    public double? Average(Expression<Func<T, int?>> selector)
        => query.Average(selector);

    public double Average(Expression<Func<T, long>> selector)
        => query.Average(selector);

    public double? Average(Expression<Func<T, long?>> selector)
        => query.Average(selector);

    public float Average(Expression<Func<T, float>> selector)
        => query.Average(selector);

    public float? Average(Expression<Func<T, float?>> selector)
        => query.Average(selector);

    public double Average(Expression<Func<T, double>> selector)
        => query.Average(selector);

    public double? Average(Expression<Func<T, double?>> selector)
        => query.Average(selector);

    public T[] ToArray() => query.ToArray();

    public List<T> ToList() => query.ToList();
}
