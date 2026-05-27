using QChain.Internal.Shapes;
using System.Linq.Expressions;

namespace QChain.Internal.Grouping;

internal static class GroupShapeBuilder<T, Q>
{
    public static IQuery<IGrouping<K, T>> CreateRaw<K>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> selector) =>
        RawGroupShapeBuilder<T, Q>.Create(query, selector);

    public static IQuery<IGrouping<K, E>> CreateRaw<K, E>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> selector,
        Expression<Func<T, E>> elementSelector) =>
        RawGroupShapeBuilder<T, Q>.Create(query, selector, elementSelector);

    public static IQuery<R> CreateProjected<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> key,
        Expression<Func<IGrouping<K, T>, R>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            query.Source,
            query.Translate(key),
            element,
            GroupResultSelectorTranslator<T, Q>.TranslateGroup(query, selector));
    }

    public static IQuery<R> CreateProjected<K, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> key,
        Expression<Func<K, IEnumerable<T>, R>> resultsSelector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder<T, Q>.Create(
            query.Source,
            query.Translate(key),
            element,
            GroupResultSelectorTranslator<T, Q>.TranslateInternalElementGroup(query, resultsSelector));
    }

    public static IQuery<R> CreateProjected<K, E, R>(
        SequenceQueryShape<T, Q> query,
        Expression<Func<T, K>> key,
        Expression<Func<T, E>> elementSelector,
        Expression<Func<K, IEnumerable<E>, R>> resultsSelector) =>
        ProjectedGroupQueryBuilder<T, Q>.Create(
            query.Source,
            query.Translate(key),
            query.Translate(elementSelector),
            GroupResultSelectorTranslator<T, Q>.TranslateElementGroup(resultsSelector));

    public static IQuery<R> CreateProjected<K, E, R>(
        IQueryable<Q> source,
        Expression<Func<Q, K>> key,
        Expression<Func<Q, E>> element,
        Expression<Func<IGrouping<K, E>, R>> shape) =>
        ProjectedGroupQueryBuilder<T, Q>.Create(source, key, element, shape);
}
