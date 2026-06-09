using PCompose.Visitors;
using QChain.Internal.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Visitors;

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
        if (TryTranslateGroupMaterializer(node, out var materialized))
            return materialized;

        var obj = Visit(node.Object);
        var args = node.Arguments.Select(VisitMethodArgument).ToArray();
        var method = RewriteMethod(node.Method, obj, args);

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

        if (expr is not null &&
            node.Member.DeclaringType is not null &&
            !node.Member.DeclaringType.IsAssignableFrom(expr.Type) &&
            ProjectionReduction.TryRewriteTupleAccess(expr, node.Member.Name, out var rewritten))
        {
            return Visit(rewritten);
        }

        if (expr is not null &&
            node.Member.DeclaringType is not null &&
            !node.Member.DeclaringType.IsAssignableFrom(expr.Type) &&
            expr.Type.IsGenericType &&
            expr.Type.GetGenericTypeDefinition() == typeof(Projection<,>) &&
            node.Member.Name is nameof(Projection<int, int>.Item1) or nameof(Projection<int, int>.Item2))
        {
            return Expression.PropertyOrField(expr, node.Member.Name);
        }

        return node.Update(expr);
    }

    private Expression VisitMethodArgument(Expression arg)
    {
        var visited = Visit(arg)!;

        if (visited is LambdaExpression lambda &&
            lambda.Parameters.Count == 1 &&
            lambda.Parameters[0].Type == typeof(T))
        {
            return TranslateElementLambda(lambda);
        }

        return visited;
    }

    private LambdaExpression TranslateElementLambda(LambdaExpression lambda)
    {
        var qParam = Expression.Parameter(typeof(Q), lambda.Parameters[0].Name);
        var shapedBody = ReplaceExpressionVisitor.Replace(_shape.Body, _shape.Parameters[0], qParam);
        var replacedBody = ReplaceExpressionVisitor.Replace(lambda.Body, lambda.Parameters[0], shapedBody);
        var finalBody = Visit(replacedBody)!;
        finalBody = TupleExpressionNormalizer.Normalize(finalBody);

        return Expression.Lambda(finalBody, qParam);
    }

    private bool TryTranslateGroupMaterializer(MethodCallExpression node, out Expression translated)
    {
        translated = null!;

        if (node.Method.DeclaringType != typeof(Enumerable) ||
            !node.Method.IsGenericMethod ||
            node.Arguments.Count != 1 ||
            node.Arguments[0] != _groupTParam)
        {
            return false;
        }

        var definition = node.Method.GetGenericMethodDefinition();
        var publicElements = SelectPublicElements();

        if (definition == EnumerableToArrayMethod)
        {
            translated = Expression.Call(
                EnumerableToArrayMethod.MakeGenericMethod(typeof(T)),
                publicElements);
            return true;
        }

        if (definition == EnumerableToListMethod)
        {
            translated = Expression.Call(
                EnumerableToListMethod.MakeGenericMethod(typeof(T)),
                publicElements);
            return true;
        }

        if (definition == EnumerableAsEnumerableMethod)
        {
            translated = publicElements;
            return true;
        }

        return false;
    }

    private MethodCallExpression SelectPublicElements() =>
        Expression.Call(
            EnumerableSelectMethod.MakeGenericMethod(typeof(Q), typeof(T)),
            _groupQParam,
            _shape);

    private static MethodInfo RewriteMethod(MethodInfo method, Expression? obj, IReadOnlyList<Expression> args)
    {
        if (!method.IsGenericMethod)
            return method;

        if (CanCall(method, obj, args))
            return method;

        var definition = method.GetGenericMethodDefinition();
        var rewrittenArguments = method
            .GetGenericArguments()
            .Select(RewriteType)
            .ToArray();

        return definition.MakeGenericMethod(rewrittenArguments);
    }

    private static bool CanCall(MethodInfo method, Expression? obj, IReadOnlyList<Expression> args)
    {
        if (obj is not null && !method.DeclaringType!.IsAssignableFrom(obj.Type))
            return false;

        var parameters = method.GetParameters();
        if (parameters.Length != args.Count)
            return false;

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
                parameterType = parameterType.GetElementType()!;

            if (!parameterType.IsAssignableFrom(args[i].Type))
                return false;
        }

        return true;
    }

    private static Type RewriteType(Type type)
    {
        if (type == typeof(T))
            return typeof(Q);

        if (type.IsArray)
        {
            var elementType = RewriteType(type.GetElementType()!);
            return type.GetArrayRank() == 1
                ? elementType.MakeArrayType()
                : elementType.MakeArrayType(type.GetArrayRank());
        }

        if (!type.IsGenericType)
            return type;

        var definition = type.GetGenericTypeDefinition();
        var arguments = type.GetGenericArguments().Select(RewriteType).ToArray();
        return definition.MakeGenericType(arguments);
    }

    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));

    private static readonly MethodInfo EnumerableToArrayMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.ToArray) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters().Length == 1);

    private static readonly MethodInfo EnumerableToListMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.ToList) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters().Length == 1);

    private static readonly MethodInfo EnumerableAsEnumerableMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.AsEnumerable) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters().Length == 1);
}
