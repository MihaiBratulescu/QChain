using QChain.Predicates;
using System.Linq.Expressions;

namespace QChain;

public interface ISyncQueryExecutor<T>
{
    #region Any/All
    public bool Any();
    public bool Any(Expression<Func<T, bool>> predicate);
    public bool Any(Func<T, Predicate> predicate);
    public bool All(Expression<Func<T, bool>> predicate);
    public bool All(Func<T, Predicate> predicate);
    #endregion

    #region Count/LongCount
    public int Count();
    public int Count(Expression<Func<T, bool>> predicate);
    public int Count(Func<T, Predicate> predicate);
    public long LongCount();
    public long LongCount(Expression<Func<T, bool>> predicate);
    public long LongCount(Func<T, Predicate> predicate);
    #endregion

    #region ElementAt/ElementAtOrDefault
    public T ElementAt(int index);
    public T? ElementAtOrDefault(int index);
    #endregion

    #region First/FirstOrDefault
    public T First();
    public T First(Expression<Func<T, bool>> predicate);
    public T First(Func<T, Predicate> predicate);
    public T? FirstOrDefault();
    public T? FirstOrDefault(Expression<Func<T, bool>> predicate);
    public T? FirstOrDefault(Func<T, Predicate> predicate);
    #endregion

    #region Last/LastOrDefault
    public T Last();
    public T Last(Expression<Func<T, bool>> predicate);
    public T Last(Func<T, Predicate> predicate);
    public T? LastOrDefault();
    public T? LastOrDefault(Expression<Func<T, bool>> predicate);
    public T? LastOrDefault(Func<T, Predicate> predicate);
    #endregion

    #region Single/SingleOrDefault
    public T Single();
    public T Single(Expression<Func<T, bool>> predicate);
    public T Single(Func<T, Predicate> predicate);
    public T? SingleOrDefault();
    public T? SingleOrDefault(Expression<Func<T, bool>> predicate);
    public T? SingleOrDefault(Func<T, Predicate> predicate);
    #endregion

    #region Min/Max
    public T? Min();
    public R? Min<R>(Expression<Func<T, R>> selector);
    public T? Max();
    public R? Max<R>(Expression<Func<T, R>> selector);
    #endregion

    #region Sum
    public decimal Sum(Expression<Func<T, decimal>> selector);
    public decimal? Sum(Expression<Func<T, decimal?>> selector);

    public int Sum(Expression<Func<T, int>> selector);
    public int? Sum(Expression<Func<T, int?>> selector);

    public long Sum(Expression<Func<T, long>> selector);
    public long? Sum(Expression<Func<T, long?>> selector);

    public float Sum(Expression<Func<T, float>> selector);
    public float? Sum(Expression<Func<T, float?>> selector);

    public double Sum(Expression<Func<T, double>> selector);
    public double? Sum(Expression<Func<T, double?>> selector);
    #endregion

    #region Average
    public decimal Average(Expression<Func<T, decimal>> selector);
    public decimal? Average(Expression<Func<T, decimal?>> selector);

    public float Average(Expression<Func<T, float>> selector);
    public float? Average(Expression<Func<T, float?>> selector);

    public double Average(Expression<Func<T, double>> selector);
    public double? Average(Expression<Func<T, double?>> selector);
    #endregion

    #region ToList/Array
    public T[] ToArray();
    public List<T> ToList();
    #endregion
}
