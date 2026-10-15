using System.Linq.Expressions;

namespace QChain;

internal class QueryProvider(IQueryProvider provider, IQueryShape queryShape) : IQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type
            .GetInterfaces()
            .Append(expression.Type)
            .First(t => t.IsGenericType &&
                        t.GetGenericTypeDefinition() == typeof(IQueryable<>))
            .GetGenericArguments()[0];

        var queryType = typeof(Query<>).MakeGenericType(elementType);

        return (IQueryable)Activator.CreateInstance(
            queryType, new QueryProvider(provider, queryShape.Apply((MethodCallExpression)expression)), expression)!;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var nextShape = queryShape.Apply((MethodCallExpression)expression);
        return new Query<TElement>(new QueryProvider(provider, nextShape), expression);
    }

    public object? Execute(Expression expression)
        => provider.Execute(TranslateExecution(expression));

    public TResult Execute<TResult>(Expression expression)
        => provider.Execute<TResult>(TranslateExecution(expression));

    private Expression TranslateExecution(Expression expression)
    {
        var projected = queryShape.Project().Expression;
        if (typeof(IQueryable).IsAssignableFrom(expression.Type))
            return projected;

        if (expression is MethodCallExpression call && call.Method.DeclaringType == typeof(Queryable))
            return call.Update(call.Object, new[] { projected }.Concat(call.Arguments.Skip(1)));

        throw new NotSupportedException("Unsupported execution expression.");
    }
}
