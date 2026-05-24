using QChain.Visitors;
using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    //IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>
    public IQuery<(K Key, IEnumerable<T> Items)> GroupBy<K>(Expression<Func<T, K>> selector) =>
        new DeferredQuery<(K, IEnumerable<T>), IGrouping<K, Q>>(
            Source.GroupBy(Translate(selector)),
            g => new ValueTuple<K, IEnumerable<T>>(g.Key, g.AsQueryable().Select(Shape).AsEnumerable()));

    //TO DO: IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>

    //NEW
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector) =>
        GroupBy(key, TranslateGroupSelector(selector));

    //IQueryable<TResult> GroupBy<TSource, TKey, TResult>
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<K, IEnumerable<T>, R>> resultsSelector) =>
        GroupBy(key, x => x, resultsSelector);

    //IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>
    public IQuery<R> GroupBy<K, E, R>(Expression<Func<T, K>> key, Expression<Func<T, E>> elementSelector, Expression<Func<K, IEnumerable<E>, R>> resultsSelector) =>
        new DeferredQuery<R, IGrouping<K, E>>(
            Source.GroupBy(Translate(key), Translate(elementSelector)), 
            TranslateElementGroup(resultsSelector));

    #region Helpers

    private static Expression<Func<K, IEnumerable<T>, R>> TranslateGroupSelector<K, R>(Expression<Func<IGrouping<K, T>, R>> selector)
    {
        var key = Expression.Parameter(typeof(K), "key");
        var items = Expression.Parameter(typeof(IEnumerable<T>), selector.Parameters[0].Name);

        var body = new GroupSelectorToResultsSelectorVisitor<K, T>(selector.Parameters[0], key, items)
            .Visit(selector.Body)!;

        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<K, IEnumerable<T>, R>>(body, key, items);
    }

    private static Expression<Func<IGrouping<K, E>, R>> TranslateElementGroup<K, E, R>(Expression<Func<K, IEnumerable<E>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, E>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, E>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], group).Visit(body)!;
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<K, E>, R>>(body, group);
    }

    private sealed class GroupSelectorToResultsSelectorVisitor<K, E>(
        ParameterExpression group,
        ParameterExpression key,
        ParameterExpression items) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == group ? items : base.VisitParameter(node);

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.Name == nameof(IGrouping<K, E>.Key) &&
                node.Expression == group)
            {
                return key;
            }

            return base.VisitMember(node);
        }
    }
    #endregion
}
