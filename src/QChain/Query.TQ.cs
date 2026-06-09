using QChain.Internal.Grouping;
using QChain.Internal.Operations;
using QChain.Internal.Shapes;
using PCompose;
using System.Linq.Expressions;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IUntypedQuery
{
    private SequenceQueryShape<T, Q> QueryShape { get; set; }

    IQueryShape IUntypedQuery.Untyped => QueryShape;

    #region Constructors
    internal Query(IQueryable<Q> source, Expression<Func<Q, T>> shape) =>
        QueryShape = new SequenceQueryShape<T, Q>(source, shape);

    internal Query(SequenceQueryShape<T, Q> queryShape) => 
        QueryShape = queryShape;

    protected Query(Query<T, Q> query) =>
        QueryShape = query.QueryShape;
    #endregion

    public IQueryable<T> AsQueryable() => QueryShape.Project();

    public IQuery<T> Distinct() =>
        Next<T>(QueryShape.Distinct());

    public IQuery<T?> DefaultIfEmpty() =>
        Next<T?>(DefaultIfEmptyOperation.Apply(QueryShape));

    #region Filtering
    public IQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        Next(QueryShape.Where(predicate));

    public IQuery<T> Where(Func<T, Predicate> predicate) =>
        Where(predicate.Compile());
    #endregion

    #region Grouping
    public IQuery<IGrouping<K, T>> GroupBy<K>(Expression<Func<T, K>> key) =>
        GroupShapeBuilder<T, Q>.CreateRaw(QueryShape, key);

    public IQuery<IGrouping<K, E>> GroupBy<K, E>(
        Expression<Func<T, K>> keySelector,
        Expression<Func<T, E>> elementSelector) =>
        GroupShapeBuilder<T, Q>.CreateRaw(QueryShape, keySelector, elementSelector);

    public IQuery<R> GroupBy<K, R>(
        Expression<Func<T, K>> key,
        Expression<Func<IGrouping<K, T>, R>> selector) =>
        GroupShapeBuilder<T, Q>.CreateProjected(QueryShape, key, selector);

    public IQuery<R> GroupBy<K, R>(
        Expression<Func<T, K>> keySelector,
        Expression<Func<K, IEnumerable<T>, R>> resultsSelector) =>
        GroupShapeBuilder<T, Q>.CreateProjected(QueryShape, keySelector, resultsSelector);

    public IQuery<R> GroupBy<K, E, R>(
        Expression<Func<T, K>> keySelector,
        Expression<Func<T, E>> elementSelector,
        Expression<Func<K, IEnumerable<E>, R>> resultsSelector) =>
        GroupShapeBuilder<T, Q>.CreateProjected(QueryShape, keySelector, elementSelector, resultsSelector);
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
    public IQuery<R> Select<R>(Expression<Func<T, R>> mapping) =>
        Next<R>(QueryShape.Select(mapping));

    public IQuery<R> SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
        Next<R>(QueryShape.SelectMany(collectionSelector));

    public IQuery<R> SelectMany<C, R>(Expression<Func<T, IEnumerable<C>>> collectionSelector, Expression<Func<T, C, R>> resultSelector) =>
        Next(QueryShape.SelectMany(collectionSelector, resultSelector));
    #endregion

    #region Sets
    public IQuery<T> Union(IQuery<T> other) =>
        Next<T>(QueryShape.Union(((IUntypedQuery)other).Untyped));

    public IQuery<T> Concat(IQuery<T> other) =>
        Next<T>(QueryShape.Concat(((IUntypedQuery)other).Untyped));

    public IQuery<T> Except(IQuery<T> other) =>
        Next<T>(QueryShape.Except(((IUntypedQuery)other).Untyped));

    public IQuery<T> ExceptBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector) =>
        Next(QueryShape.ExceptBy(keys, keySelector));

    public IQuery<T> Intersect(IQuery<T> other) =>
        Next<T>(QueryShape.Intersect(((IUntypedQuery)other).Untyped));

    public IQuery<T> IntersectBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector) =>
        Next(QueryShape.IntersectBy(keys, keySelector));
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

    private Query<TNext, QNext> Next<TNext, QNext>(SequenceQueryShape<TNext, QNext> queryShape)
        => new(queryShape);

    private IQuery<TNext> Next<TNext>(IQueryShape queryShape)
    {
        var generic = NextUntypedMethod.MakeGenericMethod(typeof(TNext), queryShape.SourceType);
        return (IQuery<TNext>)generic.Invoke(this, [queryShape])!;
    }

    private static readonly System.Reflection.MethodInfo NextUntypedMethod =
        typeof(Query<T, Q>).GetMethod(nameof(NextUntyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private Query<TNext, QNext> NextUntyped<TNext, QNext>(IQueryShape queryShape) =>
        new((SequenceQueryShape<TNext, QNext>)queryShape);
}
