using System.Linq.Expressions;

namespace QChain;

internal class QueryProvider(IQueryProvider provider) : IQueryProvider
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
            queryType, this, expression)!;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new Query<TElement>(this, expression);

    public object? Execute(Expression expression)
        => provider.Execute(expression);

    public TResult Execute<TResult>(Expression expression)
        => provider.Execute<TResult>(expression);
}
