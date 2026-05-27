using System.Linq.Expressions;

namespace QChain;

public static class SyncExtensions
{
    extension<T>(IQuery<T> query)
    {
        #region Any/All
        public bool Any() =>
            Query(query, q => q.Any());
        public bool Any(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.Any());
        public bool All(Expression<Func<T, bool>> predicate) =>
            Query(query, q => q.All(predicate));
        #endregion

        #region Count/LongCount
        public int Count() =>
            Query(query, q => q.Count());
        public int Count(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.Count());
        public long LongCount() =>
            Query(query, q => q.LongCount());
        public long LongCount(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.LongCount());
        #endregion

        #region ElementAt/ElementAtOrDefault
        public T ElementAt(int index) =>
            query.AsQueryable().ElementAt(index);

        public T? ElementAtOrDefault(int index) =>
            query.AsQueryable().ElementAtOrDefault(index);
        #endregion

        #region First/FirstOrDefault
        public T First() =>
            Query(query, q => q.First());
        public T First(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.First());
        public T? FirstOrDefault() =>
            Query(query, q => q.FirstOrDefault());
        public T? FirstOrDefault(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.FirstOrDefault());
        #endregion

        #region Last/LastOrDefault
        public T Last() =>
            Query(query, q => q.Last());
        public T Last(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.Last());
        public T? LastOrDefault() =>
            Query(query, q => q.LastOrDefault());
        public T? LastOrDefault(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.LastOrDefault());
        #endregion

        #region Single/SingleOrDefault
        public T Single() =>
            Query(query, q => q.Single());
        public T Single(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.Single());
        public T? SingleOrDefault() =>
            Query(query, q => q.SingleOrDefault());
        public T? SingleOrDefault(Expression<Func<T, bool>> predicate) =>
            Query(query, predicate, q => q.SingleOrDefault());
        #endregion

        #region Min/Max
        public T? Min() =>
            Query(query, q => q.Min());
        public R? Min<R>(Expression<Func<T, R>> selector) =>
            Query(query.Select(selector), q => q.Min());
        public T? Max() =>
            Query(query, q => q.Max());
        public R? Max<R>(Expression<Func<T, R>> selector) =>
            Query(query.Select(selector), q => q.Max());
        #endregion

        #region Sum
        public decimal Sum(Expression<Func<T, decimal>> selector) =>
            Query(query, q => q.Sum(selector));
        public decimal? Sum(Expression<Func<T, decimal?>> selector) =>
            Query(query, q => q.Sum(selector));

        public int Sum(Expression<Func<T, int>> selector) =>
          Query(query, q => q.Sum(selector));
        public int? Sum(Expression<Func<T, int?>> selector) =>
            Query(query, q => q.Sum(selector));

        public long Sum(Expression<Func<T, long>> selector) =>
          Query(query, q => q.Sum(selector));
        public long? Sum(Expression<Func<T, long?>> selector) =>
            Query(query, q => q.Sum(selector));

        public float Sum(Expression<Func<T, float>> selector) =>
          Query(query, q => q.Sum(selector));
        public float? Sum(Expression<Func<T, float?>> selector) =>
            Query(query, q => q.Sum(selector));

        public double Sum(Expression<Func<T, double>> selector) =>
          Query(query, q => q.Sum(selector));
        public double? Sum(Expression<Func<T, double?>> selector) =>
            Query(query, q => q.Sum(selector));
        #endregion

        #region Average
        public decimal Average(Expression<Func<T, decimal>> selector) =>
            Query(query, q => q.Average(selector));
        public decimal? Average(Expression<Func<T, decimal?>> selector) =>
            Query(query, q => q.Average(selector));

        public double Average(Expression<Func<T, int>> selector) =>
            Query(query, q => q.Average(selector));
        public double? Average(Expression<Func<T, int?>> selector) =>
            Query(query, q => q.Average(selector));

        public double Average(Expression<Func<T, long>> selector) =>
            Query(query, q => q.Average(selector));
        public double? Average(Expression<Func<T, long?>> selector) =>
            Query(query, q => q.Average(selector));

        public float Average(Expression<Func<T, float>> selector) =>
          Query(query, q => q.Average(selector));
        public float? Average(Expression<Func<T, float?>> selector) =>
            Query(query, q => q.Average(selector));

        public double Average(Expression<Func<T, double>> selector) =>
          Query(query, q => q.Average(selector));
        public double? Average(Expression<Func<T, double?>> selector) =>
            Query(query, q => q.Average(selector));
        #endregion

        #region ToList/Array
        public T[] ToArray() => Query(query, q => q.ToArray());
        public List<T> ToList() => Query(query, q => q.ToList());
        #endregion

        public bool Contains(T item) =>
            Query(query, q => q.Contains(item));

        #region Helpers
        internal R Query<R>(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, R> executor) =>
            executor(query.Where(predicate).AsQueryable());
        internal R Query<R>(Func<IQueryable<T>, R> executor) =>
            executor(query.AsQueryable());
        #endregion
    }
}
