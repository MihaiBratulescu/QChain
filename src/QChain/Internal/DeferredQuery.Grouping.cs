using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal;

internal class GroupedQuery<T, Q> : DeferredQuery<T, Q>
{
    internal GroupedQuery(IQueryable<Q> source, Expression<Func<Q, T>> shape) : base(source, shape)
    {
    }
}

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    //IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>
    public IQuery<(K Key, IEnumerable<T> Items)> GroupBy<K>(Expression<Func<T, K>> selector) =>
        new DeferredQuery<(K, IEnumerable<T>), IGrouping<K, Q>>(
            Source.GroupBy(Translate(selector)),
            g => new ValueTuple<K, IEnumerable<T>>(g.Key, g.AsQueryable().Select(Shape).AsEnumerable()));

    //TO DO: IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>

    //NEW
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder.Create(
            Source,
            Translate(key),
            element,
            TranslateGroup(selector));
    }

    //IQueryable<TResult> GroupBy<TSource, TKey, TResult>
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<K, IEnumerable<T>, R>> resultsSelector) =>
        GroupBy(key, x => x, resultsSelector);

    //IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>
    public IQuery<R> GroupBy<K, E, R>(Expression<Func<T, K>> key, Expression<Func<T, E>> elementSelector, Expression<Func<K, IEnumerable<E>, R>> resultsSelector)
    {
        var translatedKey = Translate(key);
        var translatedElement = Translate(elementSelector);
        var shape = TranslateElementGroup(resultsSelector);

        return ProjectedGroupQueryBuilder.Create(Source, translatedKey, translatedElement, shape);
    }

    #region Helpers

    private Expression<Func<IGrouping<K, Q>, R>> TranslateGroup<K, R>(Expression<Func<IGrouping<K, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<K, Q>), selector.Parameters[0].Name);

        var body = new GroupTranslateVisitor<K, Q, T>(groupQ, selector.Parameters[0], Shape).Visit(selector.Body);
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, groupQ);
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
    #endregion

    private static class ProjectedGroupQueryBuilder
    {
        public static IQuery<R> Create<K, E, R>(
            IQueryable<Q> source,
            Expression<Func<Q, K>> key,
            Expression<Func<Q, E>> element,
            Expression<Func<IGrouping<K, E>, R>> shape)
        {
            var loweredKey = TupleProjection.Lower(key.Body);
            var keyLambda = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(typeof(Q), loweredKey.Type),
                loweredKey,
                key.Parameters);

            var group = Expression.Parameter(typeof(IGrouping<,>).MakeGenericType(loweredKey.Type, typeof(E)), shape.Parameters[0].Name);
            var keyShape = TupleProjection.Rebuild(Expression.Property(group, nameof(IGrouping<int, int>.Key)), typeof(K));
            var publicShapeBody = new GroupKeyProjectionVisitor(shape.Parameters[0], group, keyShape).Visit(shape.Body)!;

            publicShapeBody = new ValueTupleCreateToCtorVisitor().Visit(publicShapeBody)!;
            publicShapeBody = new TupleAccessSimplifyingVisitor().Visit(publicShapeBody)!;

            var loweredResult = TupleProjection.Lower(publicShapeBody);
            var projectionLambda = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(group.Type, loweredResult.Type),
                loweredResult,
                group);

            var carrier = Expression.Parameter(loweredResult.Type, "p");
            var rebuilt = TupleProjection.Rebuild(carrier, typeof(R));
            var shapeLambda = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(loweredResult.Type, typeof(R)),
                rebuilt,
                carrier);

            return (IQuery<R>)CreateProjectedGroupQueryMethod
                .MakeGenericMethod(loweredKey.Type, typeof(E), typeof(R), loweredResult.Type)
                .Invoke(null, [source, keyLambda, element, projectionLambda, shapeLambda])!;
        }

        private static DeferredQuery<R, C> CreateProjectedGroupQuery<KInternal, E, R, C>(
            IQueryable<Q> source,
            LambdaExpression key,
            Expression<Func<Q, E>> element,
            LambdaExpression projection,
            LambdaExpression shape)
        {
            return new DeferredQuery<R, C>(
                source
                    .GroupBy((Expression<Func<Q, KInternal>>)key, element)
                    .Select((Expression<Func<IGrouping<KInternal, E>, C>>)projection),
                (Expression<Func<C, R>>)shape);
        }

        private static readonly MethodInfo CreateProjectedGroupQueryMethod =
            typeof(ProjectedGroupQueryBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(m => m.Name == nameof(CreateProjectedGroupQuery) && m.GetGenericArguments().Length == 4);

        private sealed class GroupKeyProjectionVisitor(
            ParameterExpression publicGroup,
            ParameterExpression internalGroup,
            Expression keyShape) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) =>
                node == publicGroup ? internalGroup : base.VisitParameter(node);

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == publicGroup &&
                    node.Member.Name == nameof(IGrouping<int, int>.Key))
                {
                    return keyShape;
                }

                return base.VisitMember(node);
            }
        }
    }

    private static class TupleProjection
    {
        public static Expression Lower(Expression expression)
        {
            if (!TryGetValueTuple2(expression.Type, out var leftType, out var rightType))
                return expression;

            var left = Lower(GetTupleItem(expression, 1));
            var right = Lower(GetTupleItem(expression, 2));
            var projectionType = MakeProjectionType(left.Type, right.Type);

            return Expression.MemberInit(
                Expression.New(projectionType),
                Expression.Bind(projectionType.GetProperty(nameof(Projection<int, int>.Item1))!, left),
                Expression.Bind(projectionType.GetProperty(nameof(Projection<int, int>.Item2))!, right));
        }

        public static Expression Rebuild(Expression expression, Type targetType)
        {
            if (!TryGetValueTuple2(targetType, out var leftType, out var rightType))
                return expression;

            var left = Rebuild(Expression.PropertyOrField(expression, nameof(Projection<int, int>.Item1)), leftType);
            var right = Rebuild(Expression.PropertyOrField(expression, nameof(Projection<int, int>.Item2)), rightType);

            return Expression.New(targetType.GetConstructor([leftType, rightType])!, left, right);
        }

        private static Expression GetTupleItem(Expression tuple, int item)
        {
            if (!ProjectionReduction.TryRewriteTupleAccess(tuple, $"Item{item}", out var value))
                value = Expression.PropertyOrField(tuple, $"Item{item}");

            return value;
        }

        private static bool TryGetValueTuple2(Type type, out Type left, out Type right)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
            {
                var args = type.GetGenericArguments();
                left = args[0];
                right = args[1];
                return true;
            }

            left = null!;
            right = null!;
            return false;
        }

        private static Type MakeProjectionType(Type left, Type right)
        {
            var definition = typeof(Projection<int, int>).GetGenericTypeDefinition();
            var arguments = definition.GetGenericArguments().Length == 2
                ? [left, right]
                : new[] { typeof(T), typeof(Q), left, right };

            return definition.MakeGenericType(arguments);
        }
    }
}
