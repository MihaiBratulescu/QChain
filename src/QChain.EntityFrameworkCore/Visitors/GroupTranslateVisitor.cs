using System.Linq.Expressions;
using System.Reflection;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class GroupTranslateVisitor<G, Q, T> : ExpressionVisitor
{
    private readonly Expression<Func<Q, T>> _shape;
    private readonly ParameterExpression _groupQParam;
    private readonly ParameterExpression _groupTParam;

    public GroupTranslateVisitor(ParameterExpression groupQParam, ParameterExpression groupTParam, Expression<Func<Q, T>> shape)
    {
        _groupQParam = groupQParam;
        _groupTParam = groupTParam;
        _shape = shape;
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _groupTParam ? _groupQParam : base.VisitParameter(node);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var obj = Visit(node.Object);
        var args = node.Arguments.Select(VisitMethodArgument).ToArray();
        var method = RewriteMethod(node.Method);

        return Expression.Call(obj, method, args);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var expr = Visit(node.Expression);

        if (node.Member.Name == nameof(IGrouping<int, int>.Key) &&
            expr is not null &&
            expr.Type.IsGenericType &&
            expr.Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            return Expression.Property(expr, nameof(IGrouping<int, int>.Key));
        }

        if (expr is not null && expr != node.Expression)
        {
            if (ProjectionReduction.TryInlineMemberAccess(expr, node.Member, out var rewritten))
                return Visit(rewritten);

            if (node.Member is PropertyInfo property)
                return Expression.Property(expr, property.Name);

            if (node.Member is FieldInfo field)
                return Expression.Field(expr, field.Name);
        }

        return node.Update(expr);
    }

    private Expression VisitMethodArgument(Expression arg)
    {
        var visited = Visit(arg)!;
        var unquoted = StripQuotes(visited);

        if (unquoted is LambdaExpression lambda &&
            lambda.Parameters.Count == 1 &&
            lambda.Parameters[0].Type == typeof(T))
        {
            var translated = TranslateElementLambda(lambda);
            return visited.NodeType == ExpressionType.Quote ? Expression.Quote(translated) : translated;
        }

        return visited;
    }

    private LambdaExpression TranslateElementLambda(LambdaExpression lambda)
    {
        var qParam = Expression.Parameter(typeof(Q), lambda.Parameters[0].Name);
        var shapedBody = ReplaceExpressionVisitor.Replace(_shape.Body, _shape.Parameters[0], qParam);
        var replacedBody = ReplaceExpressionVisitor.Replace(lambda.Body, lambda.Parameters[0], shapedBody);
        var finalBody = Visit(replacedBody)!;

        return Expression.Lambda(finalBody, qParam);
    }

    private static MethodInfo RewriteMethod(MethodInfo method)
    {
        if (!method.IsGenericMethod)
            return method;

        var definition = method.GetGenericMethodDefinition();
        var rewrittenArguments = method
            .GetGenericArguments()
            .Select(RewriteType)
            .ToArray();

        return definition.MakeGenericMethod(rewrittenArguments);
    }

    private static Type RewriteType(Type type)
    {
        if (type == typeof(T))
            return typeof(Q);

        if (type.IsByRef)
            return RewriteType(type.GetElementType()!).MakeByRefType();

        if (type.IsArray)
            return RewriteType(type.GetElementType()!).MakeArrayType(type.GetArrayRank());

        if (!type.IsGenericType)
            return type;

        var definition = type.GetGenericTypeDefinition();
        var arguments = type.GetGenericArguments().Select(RewriteType).ToArray();
        return definition.MakeGenericType(arguments);
    }

    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
            expression = ((UnaryExpression)expression).Operand;

        return expression;
    }
}