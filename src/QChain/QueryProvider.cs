using QChain.Internal.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain;

internal sealed class QueryProvider(IQueryProvider provider, IQueryShape queryShape) : IQueryProvider
{
    internal IQueryShape QueryShape { get; } = queryShape;

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetInterfaces().Append(expression.Type)
            .First(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            .GetGenericArguments()[0];
        return (IQueryable)QueryReflection.Invoke(CreateQueryMethod.MakeGenericMethod(elementType), this, [expression]);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new Query<TElement>(new QueryProvider(provider, Resolve(expression, provider)));

    public object? Execute(Expression expression) => provider.Execute(TranslateExecution(expression));
    public TResult Execute<TResult>(Expression expression) => provider.Execute<TResult>(TranslateExecution(expression));

    internal Expression TranslateExecution(Expression expression)
    {
        if (typeof(IQueryable).IsAssignableFrom(expression.Type))
            return Resolve(expression, provider).Project().Expression;
        if (expression is MethodCallExpression call && call.Method.DeclaringType == typeof(Queryable))
            return Resolve(call.Arguments[0], provider).TranslateExecution(call);
        throw new NotSupportedException("Unsupported execution expression.");
    }

    internal static IQueryShape Resolve(Expression expression, IQueryProvider provider) => expression switch
    {
        ConstantExpression { Value: IShapedQuery query } => query.QueryShape,
        ConstantExpression { Value: IQueryable source } => Identity(source),
        MethodCallExpression call when typeof(IQueryable).IsAssignableFrom(call.Type) => Resolve(call.Arguments[0], provider).Apply(call),
        _ => Identity(provider.CreateQuery(expression))
    };

    internal static IQueryShape Identity(IQueryable source) =>
        (IQueryShape)Activator.CreateInstance(typeof(QueryShape<>).MakeGenericType(source.ElementType), source)!;

    private static readonly MethodInfo CreateQueryMethod = typeof(QueryProvider).GetMethods()
        .Single(method => method.Name == nameof(CreateQuery) && method.IsGenericMethodDefinition);
}
