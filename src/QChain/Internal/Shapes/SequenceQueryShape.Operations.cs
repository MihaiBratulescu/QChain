using QChain.Internal.Helpers;
using QChain.Internal.Operations;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Internal.Shapes;

internal partial class SequenceQueryShape<T, Q>
{
    public override IQueryShape Apply(MethodCallExpression operation)
    {
        if (operation.Method.DeclaringType != typeof(Queryable))
            return ApplyProviderOperation(operation);

        if (operation.Method.Name == nameof(Queryable.GroupBy))
            throw new NotSupportedException("Grouping is not supported yet.");

        if (operation.Method.Name == nameof(Queryable.DefaultIfEmpty) && operation.Arguments.Count == 1)
            return DefaultIfEmptyOperation.Apply(this);

        var types = operation.Method.GetGenericArguments().Skip(1).ToArray();
        var candidates = typeof(SequenceQueryShape<T, Q>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == operation.Method.Name &&
                method.GetGenericArguments().Length == types.Length &&
                method.GetParameters().Length == operation.Arguments.Count - 1)
            .ToArray();

        foreach (var candidate in candidates)
        {
            var method = candidate.IsGenericMethodDefinition ? candidate.MakeGenericMethod(types) : candidate;
            var parameters = method.GetParameters();
            var arguments = new object?[parameters.Length];
            var matches = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                var argument = operation.Arguments[i + 1];
                arguments[i] = parameters[i].ParameterType == typeof(IQueryShape)
                    ? QueryProvider.Resolve(argument, Source.Provider)
                    : argument is UnaryExpression { NodeType: ExpressionType.Quote } quote
                        ? quote.Operand
                        : Expression.Lambda(argument).Compile().DynamicInvoke();

                if (arguments[i] is not null && !parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return (IQueryShape)QueryReflection.Invoke(method, this, arguments);
        }

        throw new NotSupportedException($"Unsupported query operation: {operation.Method.Name}.");
    }

    private IQueryShape ApplyProviderOperation(MethodCallExpression operation)
    {
        var arguments = operation.Arguments.ToArray();
        arguments[0] = Project().Expression;
        return QueryProvider.Identity(Source.Provider.CreateQuery(operation.Update(operation.Object, arguments)));
    }
}
