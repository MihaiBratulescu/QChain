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
        var method = IsLinqMethod(node.Method)
            ? RewriteMethod(node.Method)
            : node.Method;

        return Expression.Call(obj, method, args);
    }
    private static bool IsLinqMethod(MethodInfo method)
    {
        var type = method.DeclaringType;
        return type == typeof(Queryable) || type == typeof(Enumerable);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var expr = Visit(node.Expression);

        if (expr is MethodCallExpression call &&
            call.Method.DeclaringType == typeof(ValueTuple) &&
            call.Method.Name == nameof(ValueTuple.Create) &&
            TryGetTupleIndex(node.Member.Name, out var index))
        {
            return call.Arguments[index];
        }

        if (expr is NewExpression ne &&
            ne.Type.FullName!.StartsWith("System.ValueTuple`") &&
            TryGetTupleIndex(node.Member.Name, out var index2))
        {
            return ne.Arguments[index2];
        }

        return node.Update(expr);
    }

    private static bool TryGetTupleIndex(string name, out int index)
    {
        index = -1;

        if (!name.StartsWith("Item"))
            return false;

        if (!int.TryParse(name.Substring(4), out var itemNumber))
            return false;

        index = itemNumber - 1;
        return index >= 0;
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