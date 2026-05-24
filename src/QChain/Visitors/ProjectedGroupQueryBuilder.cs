using QChain.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Visitors;

internal static class ProjectedGroupQueryBuilder<T, Q>
{
    public static IQuery<R> Create<K, E, R>(
        IQueryable<Q> source,
        Expression<Func<Q, K>> key,
        Expression<Func<Q, E>> element,
        Expression<Func<IGrouping<K, E>, R>> shape)
    {
        var loweredKey = TupleProjection<T, Q>.Lower(key.Body);
        var keyLambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(Q), loweredKey.Type),
            loweredKey,
            key.Parameters);

        var group = Expression.Parameter(typeof(IGrouping<,>).MakeGenericType(loweredKey.Type, typeof(E)), shape.Parameters[0].Name);
        var keyShape = TupleProjection<T, Q>.Rebuild(Expression.Property(group, nameof(IGrouping<int, int>.Key)), typeof(K));
        var publicShapeBody = new GroupKeyProjectionVisitor(shape.Parameters[0], group, keyShape).Visit(shape.Body)!;

        publicShapeBody = new ValueTupleCreateToCtorVisitor().Visit(publicShapeBody)!;
        publicShapeBody = new TupleAccessSimplifyingVisitor().Visit(publicShapeBody)!;

        var loweredResult = TupleProjection<T, Q>.Lower(publicShapeBody);
        var projectionLambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(group.Type, loweredResult.Type),
            loweredResult,
            group);

        var carrier = Expression.Parameter(loweredResult.Type, "p");
        var rebuilt = TupleProjection<T, Q>.Rebuild(carrier, typeof(R));
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
        typeof(ProjectedGroupQueryBuilder<T, Q>).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
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
