using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using QChain.Internal.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain;

#pragma warning disable EF1001 // EF's tracking extensions require an EntityQueryProvider.

// Every entry point delegates to the wrapped provider, so the base query compiler is unused.
internal sealed class QueryProvider(IQueryProvider provider, IQueryShape queryShape) : EntityQueryProvider(null!)
{
    internal IQueryShape QueryShape { get; } = queryShape;

    public override IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetInterfaces().Append(expression.Type)
            .First(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            .GetGenericArguments()[0];
        return (IQueryable)QueryReflection.Invoke(CreateQueryMethod.MakeGenericMethod(elementType), this, [expression]);
    }

    public override IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new Query<TElement>(new QueryProvider(provider, Resolve(expression, provider)));

    public override object Execute(Expression expression) => provider.Execute(TranslateExecution(expression))!;
    public override TResult Execute<TResult>(Expression expression) => provider.Execute<TResult>(TranslateExecution(expression));

    public override TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        if (provider is not IAsyncQueryProvider asyncProvider)
            throw new InvalidOperationException("The underlying query provider does not support asynchronous execution.");

        return asyncProvider.ExecuteAsync<TResult>(TranslateExecution(expression), cancellationToken);
    }

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

#pragma warning restore EF1001
