using System.Linq.Expressions;

namespace QChain.Internal;

internal sealed class GroupedQueryShape<K, KQ, E, QG, T, Q>
    : SequenceQueryShape<IGrouping<K, E>, IGrouping<KQ, QG>>
{
    private readonly IQueryable<IGrouping<KQ, QG>> _groupedSource;
    private readonly IQueryable<Q> _originalSource;
    private readonly Expression<Func<Q, K>> _key;
    private readonly LambdaExpression _internalKey;
    private readonly Expression<Func<Q, QG>> _element;
    private readonly Expression<Func<QG, E>> _elementShape;

    internal GroupedQueryShape(
        IQueryable<IGrouping<KQ, QG>> groupedSource,
        IQueryable<Q> originalSource,
        Expression<Func<Q, K>> key,
        LambdaExpression internalKey,
        Expression<Func<Q, QG>> element,
        Expression<Func<QG, E>> elementShape,
        Expression<Func<IGrouping<KQ, QG>, IGrouping<K, E>>> shape) : base(groupedSource, shape)
    {
        _groupedSource = groupedSource;
        _originalSource = originalSource;
        _key = key;
        _internalKey = internalKey;
        _element = element;
        _elementShape = elementShape;
    }

    protected override SequenceQueryShape<IGrouping<K, E>, IGrouping<KQ, QG>> WithSource(IQueryable<IGrouping<KQ, QG>> source) =>
        new GroupedQueryShape<K, KQ, E, QG, T, Q>(source, _originalSource, _key, _internalKey, _element, _elementShape, Shape);

    public override Expression<Func<IGrouping<KQ, QG>, R>> Translate<R>(Expression<Func<IGrouping<K, E>, R>> expression) =>
        TranslateGrouped(expression);

    public override IQueryShape Compose<R>(Expression<Func<IGrouping<K, E>, R>> outer) =>
        ProjectGrouped(TranslateGroup(outer));

    private Expression<Func<IGrouping<K, QG>, R>> TranslateGroup<R>(Expression<Func<IGrouping<K, E>, R>> mapping)
    {
        var group = Expression.Parameter(typeof(IGrouping<K, QG>), mapping.Parameters[0].Name);
        var body = new Internal.Visitors.GroupTranslateVisitor<K, QG, E>(group, mapping.Parameters[0], _elementShape)
            .Visit(mapping.Body);
        body = Internal.Visitors.TupleExpressionNormalizer.Normalize(body!);

        return Expression.Lambda<Func<IGrouping<K, QG>, R>>(body, group);
    }

    private IQueryShape ProjectGrouped<R>(Expression<Func<IGrouping<K, QG>, R>> publicKeyShape)
    {
        var group = Expression.Parameter(typeof(IGrouping<KQ, QG>), publicKeyShape.Parameters[0].Name);
        var keyShape = Internal.Visitors.TupleProjection<T, Q>.Rebuild(Expression.Property(group, nameof(IGrouping<int, int>.Key)), typeof(K));
        var body = new GroupKeyRebuildVisitor(publicKeyShape.Parameters[0], group, keyShape).Visit(publicKeyShape.Body)!;
        body = Internal.Visitors.TupleExpressionNormalizer.Normalize(body);

        var lowered = Internal.Visitors.TupleProjection<T, Q>.Lower(body);
        var projection = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(IGrouping<KQ, QG>), lowered.Type),
            lowered,
            group);

        return (IQueryShape)ProjectGroupedTypedMethod
            .MakeGenericMethod(typeof(R), lowered.Type)
            .Invoke(this, [projection])!;
    }

    private SequenceQueryShape<R, C> ProjectGroupedTyped<R, C>(LambdaExpression projectionUntyped)
    {
        var projection = (Expression<Func<IGrouping<KQ, QG>, C>>)projectionUntyped;
        var carrier = Expression.Parameter(typeof(C), "p");
        var rebuilt = Internal.Visitors.TupleProjection<T, Q>.Rebuild(carrier, typeof(R));

        return new ProjectedQueryShape<R, C>(
            _groupedSource.Select(projection),
            Expression.Lambda<Func<C, R>>(rebuilt, carrier));
    }

    private Expression<Func<IGrouping<KQ, QG>, R>> TranslateGrouped<R>(Expression<Func<IGrouping<K, E>, R>> expression)
    {
        var group = Expression.Parameter(typeof(IGrouping<KQ, QG>), expression.Parameters[0].Name);
        var keyShape = Internal.Visitors.TupleProjection<T, Q>.Rebuild(Expression.Property(group, nameof(IGrouping<int, int>.Key)), typeof(K));

        var body = new Internal.Visitors.GroupTranslateVisitor<K, QG, E>(group, expression.Parameters[0], _elementShape)
            .Visit(expression.Body);
        body = new GroupKeyRebuildVisitor(expression.Parameters[0], group, keyShape).Visit(body!);
        body = Internal.Visitors.TupleExpressionNormalizer.Normalize(body!);

        return Expression.Lambda<Func<IGrouping<KQ, QG>, R>>(body, group);
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
                Internal.Visitors.ProjectionReduction.TryRewriteTupleAccess(expression, node.Member.Name, out var rewritten))
            {
                return rewritten;
            }

            return node.Update(expression);
        }
    }

    private static readonly System.Reflection.MethodInfo ProjectGroupedTypedMethod =
        typeof(GroupedQueryShape<K, KQ, E, QG, T, Q>).GetMethod(nameof(ProjectGroupedTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
}
