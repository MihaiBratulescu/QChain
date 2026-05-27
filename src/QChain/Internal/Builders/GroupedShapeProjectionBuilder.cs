using QChain.Internal.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Builders;

internal static class GroupedShapeProjectionBuilder<K, KQ, E, QG, T, Q>
{
    public static Expression<Func<IGrouping<KQ, QG>, R>> Translate<R>(
        Expression<Func<IGrouping<K, E>, R>> expression,
        Expression<Func<QG, E>> elementShape)
    {
        var group = Expression.Parameter(typeof(IGrouping<KQ, QG>), expression.Parameters[0].Name);
        var keyShape = TupleProjection<T, Q>.Rebuild(Expression.Property(group, nameof(IGrouping<int, int>.Key)), typeof(K));

        var body = new GroupTranslateVisitor<K, QG, E>(group, expression.Parameters[0], elementShape)
            .Visit(expression.Body);
        body = new GroupKeyRebuildVisitor(expression.Parameters[0], group, keyShape).Visit(body!);
        body = TupleExpressionNormalizer.Normalize(body!);

        return Expression.Lambda<Func<IGrouping<KQ, QG>, R>>(body, group);
    }

    public static IQueryShape Compose<R>(
        IQueryable<IGrouping<KQ, QG>> source,
        Expression<Func<IGrouping<K, E>, R>> outer,
        Expression<Func<QG, E>> elementShape)
    {
        var publicKeyShape = TranslatePublicKeyGroup(outer, elementShape);
        return ProjectGrouped(source, publicKeyShape);
    }

    private static Expression<Func<IGrouping<K, QG>, R>> TranslatePublicKeyGroup<R>(
        Expression<Func<IGrouping<K, E>, R>> mapping,
        Expression<Func<QG, E>> elementShape)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, QG>), mapping.Parameters[0].Name);
        var body = new GroupTranslateVisitor<K, QG, E>(group, mapping.Parameters[0], elementShape)
            .Visit(mapping.Body);
        body = TupleExpressionNormalizer.Normalize(body!);

        return Expression.Lambda<Func<IGrouping<K, QG>, R>>(body, group);
    }

    private static IQueryShape ProjectGrouped<R>(
        IQueryable<IGrouping<KQ, QG>> source,
        Expression<Func<IGrouping<K, QG>, R>> publicKeyShape)
    {
        var group = Expression.Parameter(typeof(IGrouping<KQ, QG>), publicKeyShape.Parameters[0].Name);
        var keyShape = TupleProjection<T, Q>.Rebuild(Expression.Property(group, nameof(IGrouping<int, int>.Key)), typeof(K));
        var body = new GroupKeyRebuildVisitor(publicKeyShape.Parameters[0], group, keyShape).Visit(publicKeyShape.Body)!;
        body = TupleExpressionNormalizer.Normalize(body);

        var lowered = TupleProjection<T, Q>.Lower(body);
        var projection = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(IGrouping<KQ, QG>), lowered.Type),
            lowered,
            group);

        return (IQueryShape)ProjectGroupedTypedMethod
            .MakeGenericMethod(typeof(R), lowered.Type)
            .Invoke(null, [source, projection])!;
    }

    private static SequenceQueryShape<R, C> ProjectGroupedTyped<R, C>(
        IQueryable<IGrouping<KQ, QG>> source,
        LambdaExpression projectionUntyped)
    {
        var projection = (Expression<Func<IGrouping<KQ, QG>, C>>)projectionUntyped;
        var carrier = Expression.Parameter(typeof(C), "p");
        var rebuilt = TupleProjection<T, Q>.Rebuild(carrier, typeof(R));

        return new ProjectedQueryShape<R, C>(
            source.Select(projection),
            Expression.Lambda<Func<C, R>>(rebuilt, carrier));
    }

    private sealed class GroupKeyRebuildVisitor(
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

            if (node.Expression == internalGroup &&
                node.Member.Name == nameof(IGrouping<int, int>.Key))
            {
                return keyShape;
            }

            var expression = Visit(node.Expression);

            if (expression is not null &&
                node.Member.DeclaringType is not null &&
                !node.Member.DeclaringType.IsAssignableFrom(expression.Type) &&
                ProjectionReduction.TryRewriteTupleAccess(expression, node.Member.Name, out var rewritten))
            {
                return rewritten;
            }

            return node.Update(expression);
        }
    }

    private static readonly MethodInfo ProjectGroupedTypedMethod =
        typeof(GroupedShapeProjectionBuilder<K, KQ, E, QG, T, Q>).GetMethod(nameof(ProjectGroupedTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
}
