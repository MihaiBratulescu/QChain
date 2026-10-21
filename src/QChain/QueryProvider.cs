using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using QChain.Internal.Helpers;
using QChain.Internal.Types;
using QChain.Internal.Visitors;
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

        // Element terminals must return the public type, including default(T) for empty queries.
        if (expression is MethodCallExpression call && call.Method.DeclaringType == typeof(Queryable) &&
            call.Type == CarrierProjection.SequenceElement(call.Arguments[0].Type) &&
            CarrierProjection.RewriteType(call.Type) != call.Type)
        {
            var source = call.Arguments[0];
            var arguments = call.Arguments.Skip(1).ToList();
            if (arguments.FirstOrDefault() is UnaryExpression { Operand: LambdaExpression predicate } &&
                predicate.ReturnType == typeof(bool) && call.Method.GetParameters()[1].Name == "predicate")
            {
                source = Expression.Call(typeof(Queryable), nameof(Queryable.Where), [call.Type], source, arguments[0]);
                arguments.RemoveAt(0);
            }

            return Expression.Call(typeof(Queryable), call.Method.Name, call.Method.GetGenericArguments(),
                new[] { Resolve(source, provider).Project().Expression }.Concat(arguments).ToArray());
        }

        return new TupleRewritingVisitor().Visit(expression);
    }

    internal static IQueryShape Resolve(Expression expression, IQueryProvider provider)
    {
        if (expression is ConstantExpression { Value: IShapedQuery query })
            return query.QueryShape;

        var publicType = CarrierProjection.SequenceElement(expression.Type)!;
        var source = provider.CreateQuery(new TupleRewritingVisitor().Visit(expression));
        var shapeType = publicType == source.ElementType
            ? typeof(QueryShape<>).MakeGenericType(publicType)
            : typeof(QueryShape<,>).MakeGenericType(publicType, source.ElementType);
        return (IQueryShape)Activator.CreateInstance(shapeType, source)!;
    }

    private static readonly MethodInfo CreateQueryMethod = typeof(QueryProvider).GetMethods()
        .Single(method => method.Name == nameof(CreateQuery) && method.IsGenericMethodDefinition);
}

#pragma warning restore EF1001
