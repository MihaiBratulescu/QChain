using System.Linq.Expressions;

namespace QChain.Internal;

internal abstract partial class SequenceQueryShape<T, Q>(IQueryable<Q> source, Expression<Func<Q, T>> shape) 
    : QueryShape<T, Q>(source, shape)
{
    public SequenceQueryShape<T, Q> Where(Expression<Func<T, bool>> predicate) =>
        WithSource(Source.Where(Translate(predicate)));

    public SequenceQueryShape<T, Q> Skip(int count) =>
        WithSource(Source.Skip(count));

    public SequenceQueryShape<T, Q> Take(int count) =>
        WithSource(Source.Take(count));

    public SequenceQueryShape<T, Q> Page(int index, int count) =>
        WithSource(Source.Skip(index * count).Take(count));

    public SequenceQueryShape<T, Q> OrderBy<K>(Expression<Func<T, K>> selector) =>
        WithSource(Source.OrderBy(Translate(selector)));

    public SequenceQueryShape<T, Q> OrderByDescending<K>(Expression<Func<T, K>> selector) =>
        WithSource(Source.OrderByDescending(Translate(selector)));

    public SequenceQueryShape<T, Q> ThenBy<K>(Expression<Func<T, K>> selector) =>
        WithSource(((IOrderedQueryable<Q>)Source).ThenBy(Translate(selector)));

    public SequenceQueryShape<T, Q> ThenByDescending<K>(Expression<Func<T, K>> selector) =>
        WithSource(((IOrderedQueryable<Q>)Source).ThenByDescending(Translate(selector)));

    public SequenceQueryShape<T, Q> Reverse() => WithSource(Source.Reverse());

    public IQueryShape Distinct() => SetQueryShape<T, Q>.Distinct(this);

    public IQueryShape Union(IQueryShape other) => SetQueryShape<T, Q>.Union(this, other);

    public IQueryShape Concat(IQueryShape other) => SetQueryShape<T, Q>.Concat(this, other);

    public IQueryShape Except(IQueryShape other) => SetQueryShape<T, Q>.Except(this, other);

    public IQueryShape Intersect(IQueryShape other) => SetQueryShape<T, Q>.Intersect(this, other);

    public IQueryShape Select<R>(Expression<Func<T, R>> mapping) => Compose(mapping);

    public virtual IQueryShape Compose<R>(Expression<Func<T, R>> outer) => 
        ProjectedQueryShape<T, Q>.Compose(this, outer);

    public IQueryShape SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
        ProjectedQueryShape<T, Q>.SelectMany(this, collectionSelector);

    public SequenceQueryShape<R, Pair<Q, C>> SelectMany<C, R>(
        Expression<Func<T, IEnumerable<C>>> collectionSelector,
        Expression<Func<T, C, R>> resultSelector) =>
        ProjectedQueryShape<T, Q>.SelectMany(this, collectionSelector, resultSelector);

    public IQueryShape Join<R, K, TOut>(IQueryShape right, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T, R, TOut>> result) =>
        JoinedQueryShape<T, Q>.Join(this, right, leftKey, rightKey, result);

    public IQueryShape GroupJoin<R, K, TOut>(IQueryShape right, Expression<Func<T, K>> leftKey, Expression<Func<R, K>> rightKey, Expression<Func<T, IEnumerable<R>, TOut>> result) =>
        JoinedQueryShape<T, Q>.GroupJoin(this, right, leftKey, rightKey, result);

#if NET10_0_OR_GREATER
    public IQueryShape LeftJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R?, TOut>> result) =>
        JoinedQueryShape<T, Q>.LeftJoin(this, right, leftKey, rightKey, result);

    public IQueryShape RightJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T?, R, TOut>> result) =>
        JoinedQueryShape<T, Q>.RightJoin(this, right, leftKey, rightKey, result);
#endif

    public SequenceQueryShape<T, Q> ExceptBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector) =>
        WhereKeyIn(keys, keySelector, include: false);

    public SequenceQueryShape<T, Q> IntersectBy<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector) =>
        WhereKeyIn(keys, keySelector, include: true);

    protected abstract SequenceQueryShape<T, Q> WithSource(IQueryable<Q> source);

    private SequenceQueryShape<T, Q> WhereKeyIn<K>(IEnumerable<K> keys, Expression<Func<T, K>> keySelector,bool include)
    {
        var translated = Translate(keySelector);

        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(K)],
            Expression.Constant(keys),
            translated.Body);

        Expression body = include ? contains : Expression.Not(contains);

        var predicate = Expression.Lambda<Func<Q, bool>>(
            body,
            translated.Parameters);

        return WithSource(Source.Where(predicate));
    }

}
