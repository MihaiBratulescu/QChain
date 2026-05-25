using QChain.Internal;
using QChain.Predicates;
using System.Linq.Expressions;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IUntypedQuery
{
    private QueryShape<T, Q> QueryShape { get; set; }

    IQueryShape IUntypedQuery.Untyped => QueryShape;


    #region Constructors
    internal Query(IQueryable<Q> source, Expression<Func<Q, T>> shape) =>
        QueryShape = new QueryShape<T, Q>(source, shape);

    private Query(QueryShape<T, Q> queryShape) => 
        QueryShape = queryShape;

    protected Query(Query<T, Q> query) =>
        QueryShape = query.QueryShape;
    #endregion

    public IQueryable<T> AsQueryable() => QueryShape.Project();

    public IQuery<T> Distinct() =>
        Next<T>(QueryShape.Distinct());

    public IQuery<T?> DefaultIfEmpty() =>
        new Query<T?>(QueryShape.Project().DefaultIfEmpty());

    #region Filtering
    public IQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        Next(QueryShape.Where(predicate));

    public IQuery<T> Where(Func<T, Predicate> predicate) =>
        Where(PredicateCompiler.Compile(predicate));
    #endregion

    #region Grouping
    #endregion

    #region Joins
    public IQuery<(T, R)> Join<R, K>(IQuery<R> right, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey) =>
        Join(right, leftKey, rightKey, (left, rightRow) => new ValueTuple<T, R>(left, rightRow));

    public IQuery<TOut> Join<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T, R, TOut>> result) =>
        Next<TOut>(QueryShape.Join(((IUntypedQuery)right).Untyped, leftKey, rightKey, result));

    public IQuery<(T, IEnumerable<R>)> GroupJoin<R, K>(IQuery<R> right, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey) =>
        GroupJoin(right, leftKey, rightKey, (left, rightRows) => new ValueTuple<T, IEnumerable<R>>(left, rightRows));

    public IQuery<TOut> GroupJoin<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T, IEnumerable<R>, TOut>> result) =>
        Next<TOut>(QueryShape.GroupJoin(((IUntypedQuery)right).Untyped, leftKey, rightKey, result));

#if NET10_0_OR_GREATER
    public IQuery<(T, R?)> LeftJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey) =>
        LeftJoin(other, leftKey, rightKey, (left, rightRow) => ValueTuple.Create(left, rightRow));

    public IQuery<TResult> LeftJoin<R, K, TResult>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T, R?, TResult>> resultSelector) =>
        Next<TResult>(QueryShape.LeftJoin(((IUntypedQuery)other).Untyped, leftKey, rightKey, resultSelector));

    public IQuery<(T?, R)> RightJoin<R, K>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey) =>
        RightJoin(other, leftKey, rightKey, (left, rightRow) => ValueTuple.Create(left, rightRow));

    public IQuery<TResult> RightJoin<R, K, TResult>(IQuery<R> other, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T?, R, TResult>> resultSelector) =>
        Next<TResult>(QueryShape.RightJoin(((IUntypedQuery)other).Untyped, leftKey, rightKey, resultSelector));
#endif
    #endregion

    #region Paging
    public IQuery<T> Skip(int count) =>
        Next(QueryShape.Skip(count));

    public IQuery<T> Take(int count) =>
        Next(QueryShape.Take(count));

    public IQuery<T> Page(int index, int count) =>
        Next(QueryShape.Page(index, count));
    #endregion

    #region Projection
    public virtual IQuery<R> Select<R>(Expression<Func<T, R>> mapping) =>
        Next(QueryShape.Compose(mapping));

    public IQuery<R> SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
        Next<R>(QueryShape.SelectMany(collectionSelector));

    public IQuery<R> SelectMany<C, R>(Expression<Func<T, IEnumerable<C>>> collectionSelector, Expression<Func<T, C, R>> resultSelector) =>
        Next(QueryShape.SelectMany(collectionSelector, resultSelector));
    #endregion

    #region Sets

    #endregion

    #region Sorting
    public IOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> selector) =>
        Next(QueryShape.OrderBy(selector));

    public IOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> selector) =>
        Next(QueryShape.OrderByDescending(selector));

    public IOrderedQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> selector) =>
        Next(QueryShape.ThenBy(selector));

    public IOrderedQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> selector) =>
        Next(QueryShape.ThenByDescending(selector));

    public IQuery<T> Reverse() => Next(QueryShape.Reverse());
    #endregion

    private Query<TNext, QNext> Next<TNext, QNext>(IQueryable<QNext> source, Expression<Func<QNext, TNext>> shape)
        => new(source, shape);

    private Query<TNext, QNext> Next<TNext, QNext>(QueryShape<TNext, QNext> queryShape)
        => new(queryShape);

    private IQuery<TNext> Next<TNext>(IQueryShape queryShape)
    {
        var generic = NextUntypedMethod.MakeGenericMethod(typeof(TNext), queryShape.SourceType);
        return (IQuery<TNext>)generic.Invoke(this, [queryShape])!;
    }

    private Query<TNext, QNext> NextUntyped<TNext, QNext>(IQueryShape queryShape) =>
        new((QueryShape<TNext, QNext>)queryShape);

    private static readonly System.Reflection.MethodInfo NextUntypedMethod =
        typeof(Query<T, Q>).GetMethod(nameof(NextUntyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
}
