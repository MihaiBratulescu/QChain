using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal class GroupedQuery<T, Q> : DeferredQuery<T, Q>
{
    internal GroupedQuery(IQueryable<Q> source, Expression<Func<Q, T>> shape) : base(source, shape)
    {
    }

    public IQuery<IGrouping<K, T>> GroupByInternal<K>(Expression<Func<T, K>> selector)
    {
        return null;
    }

    public IQuery<IGrouping<K, E>> GroupByInternal<K, E>(Expression<Func<T, K>> selector, 
                                                         Expression<Func<T, E>> elementSelector)
    {
        return null;
    }

    public override IQuery<R> Select<R>(Expression<Func<T, R>> mapping) =>
       new DeferredQuery<R, R>(AsQueryable().Select(mapping), x => x);
    public override IQuery<R> SelectMany<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
       new DeferredQuery<R, R>(AsQueryable().SelectMany(collectionSelector), x => x);

    public override IQuery<R> SelectMany<C, R>(Expression<Func<T, IEnumerable<C>>> collectionSelector, Expression<Func<T, C, R>> resultSelector) =>
       new DeferredQuery<R, R>(AsQueryable().SelectMany(collectionSelector, resultSelector), x => x);
}

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    //IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>
    public IQuery<IGrouping<K, T>> GroupBy<K>(Expression<Func<T, K>> selector) =>
        new GroupedQuery<T, Q>(Source, Shape).GroupByInternal(selector);

    //IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>
    public IQuery<IGrouping<K, E>> GroupBy<K, E>(Expression<Func<T, K>> keySelector, Expression<Func<T, E>> elementSelector) =>
        new GroupedQuery<T, Q>(Source, Shape).GroupByInternal(keySelector, elementSelector);

    //NEW
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            Source,
            Translate(key),
            element,
            TranslateGroup(selector));
    }

    //IQueryable<TResult> GroupBy<TSource, TKey, TResult>
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<K, IEnumerable<T>, R>> resultsSelector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            Source,
            Translate(key),
            element,
            TranslateInternalElementGroup(resultsSelector));
    }

    //IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>
    public IQuery<R> GroupBy<K, E, R>(Expression<Func<T, K>> key, Expression<Func<T, E>> elementSelector, Expression<Func<K, IEnumerable<E>, R>> resultsSelector)
    {
        var translatedKey = Translate(key);
        var translatedElement = Translate(elementSelector);
        var shape = TranslateElementGroup(resultsSelector);

        return ProjectedGroupQueryBuilder<T, Q>.Create(Source, translatedKey, translatedElement, shape);
    }

    #region Helpers
    private Expression<Func<IGrouping<K, Q>, R>> TranslateGroup<K, R>(Expression<Func<IGrouping<K, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<K, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<K, Q, T>(groupQ, selector.Parameters[0], Shape).Visit(selector.Body);
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, groupQ);
    }

    private static Expression<Func<IGrouping<K, E>, R>> TranslateElementGroup<K, E, R>(Expression<Func<K, IEnumerable<E>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, E>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, E>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], group).Visit(body)!;
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, E>, R>>(body, group);
    }

    private Expression<Func<IGrouping<K, Q>, R>> TranslateInternalElementGroup<K, R>(Expression<Func<K, IEnumerable<T>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, Q>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, Q>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], ComposeEnumerable(Shape, group)).Visit(body)!;
        body = TupleExpressionNormalizer.Normalize(body);

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, group);
    }
    #endregion
}
