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
    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<K, IEnumerable<T>, R>> resultsSelector)
    {
        Expression<Func<Q, Q>> element = q => q;

        return ProjectedGroupQueryBuilder.Create(
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

    private Expression<Func<IGrouping<K, Q>, R>> TranslateInternalElementGroup<K, R>(Expression<Func<K, IEnumerable<T>, R>> selector)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, Q>), "g");

        var body = new ReplaceExpressionVisitor(selector.Parameters[0], Expression.Property(group, nameof(IGrouping<K, Q>.Key)))
            .Visit(selector.Body)!;

        body = new ReplaceExpressionVisitor(selector.Parameters[1], ComposeEnumerable(Shape, group)).Visit(body)!;
        body = new ValueTupleCreateToCtorVisitor().Visit(body)!;
        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<IGrouping<K, Q>, R>>(body, group);
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
            if (!TryGetValueTupleItems(expression.Type, out var itemTypes))
                return expression;

            var items = itemTypes
                .Select((_, index) => Lower(GetTupleItem(expression, index + 1)))
                .ToArray();

            return BuildProjectionTree(items);
        }

        public static Expression Rebuild(Expression expression, Type targetType)
        {
            if (!TryGetValueTupleItems(targetType, out var itemTypes))
                return expression;

            var items = ReadProjectionTree(expression, itemTypes.Length)
                .Select((item, index) => Rebuild(item, itemTypes[index]))
                .ToArray();

            return Expression.New(targetType.GetConstructor(itemTypes)!, items);
        }

        private static Expression GetTupleItem(Expression tuple, int item)
        {
            if (!ProjectionReduction.TryRewriteTupleAccess(tuple, $"Item{item}", out var value))
                value = Expression.PropertyOrField(tuple, $"Item{item}");

            return value;
        }

        private static bool TryGetValueTupleItems(Type type, out Type[] items)
        {
            if (type.IsGenericType &&
                type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
            {
                var args = type.GetGenericArguments();
                if (args.Length > 7)
                    throw new NotSupportedException("ValueTuple arity > 7 not supported yet.");

                items = args;
                return true;
            }

            items = [];
            return false;
        }

        private static Expression BuildProjectionTree(IReadOnlyList<Expression> items)
        {
            if (items.Count == 2)
                return CreateProjection(items[0], items[1]);

            return CreateProjection(
                BuildProjectionTree(items.Take(items.Count - 1).ToArray()),
                items[^1]);
        }

        private static Expression[] ReadProjectionTree(Expression projection, int count)
        {
            if (count == 2)
            {
                return
                [
                    Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item1)),
                    Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item2))
                ];
            }

            var left = Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item1));
            var right = Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item2));

            return [.. ReadProjectionTree(left, count - 1), right];
        }

        private static Expression CreateProjection(Expression left, Expression right)
        {
            var projectionType = MakeProjectionType(left.Type, right.Type);

            return Expression.MemberInit(
                Expression.New(projectionType),
                Expression.Bind(projectionType.GetProperty(nameof(Projection<int, int>.Item1))!, left),
                Expression.Bind(projectionType.GetProperty(nameof(Projection<int, int>.Item2))!, right));
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
