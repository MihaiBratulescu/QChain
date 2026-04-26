using System.Linq.Expressions;
using System.Reflection;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class GroupTranslateVisitor<K, Q, T> : ExpressionVisitor
{
    private readonly ParameterExpression _targetGroup;
    private readonly ParameterExpression _sourceGroup;
    private readonly Expression<Func<Q, T>> _shape;

    public GroupTranslateVisitor(
        ParameterExpression targetGroup,
        ParameterExpression sourceGroup,
        Expression<Func<Q, T>> shape)
    {
        _targetGroup = targetGroup;
        _sourceGroup = sourceGroup;
        _shape = shape;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _sourceGroup ? _targetGroup : base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression == _sourceGroup &&
            node.Member.Name == nameof(IGrouping<K, T>.Key))
        {
            return Expression.Property(_targetGroup, nameof(IGrouping<K, Q>.Key));
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Arguments.Count > 0 && node.Arguments[0] == _sourceGroup)
        {
            return RewriteGroupEnumerableMethod(node);
        }

        return base.VisitMethodCall(node);
    }

    private Expression RewriteGroupEnumerableMethod(MethodCallExpression node)
    {
        var args = node.Arguments.ToArray();
        args[0] = _targetGroup;

        for (var i = 1; i < args.Length; i++)
        {
            args[i] = RewriteLambdaIfNeeded(args[i]);
        }

        return Expression.Call(node.Method.GetGenericMethodDefinition().MakeGenericMethod(
            node.Method.GetGenericArguments().Select(t => t == typeof(T) ? typeof(Q) : t).ToArray()
        ), args);
    }

    private Expression RewriteLambdaIfNeeded(Expression arg)
    {
        if (arg is UnaryExpression { NodeType: ExpressionType.Quote } quote &&
            quote.Operand is LambdaExpression quotedLambda)
        {
            return Expression.Quote(RewriteElementLambda(quotedLambda));
        }

        if (arg is LambdaExpression lambda)
        {
            return RewriteElementLambda(lambda);
        }

        return Visit(arg);
    }

    private LambdaExpression RewriteElementLambda(LambdaExpression lambda)
    {
        var q = Expression.Parameter(typeof(Q), lambda.Parameters[0].Name ?? "q");

        var shaped = ReplaceExpressionVisitor.Replace(
            _shape.Body,
            _shape.Parameters[0],
            q);

        var body = ReplaceExpressionVisitor.Replace(
            lambda.Body,
            lambda.Parameters[0],
            shaped);

        return Expression.Lambda(body, q);
    }
}
