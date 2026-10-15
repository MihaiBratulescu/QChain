using System.Linq.Expressions;
using QChain.Visitors;

namespace QChain;

internal interface IQueryShape
{
    IQueryable Source { get; }
    LambdaExpression Shape { get; }
    IQueryShape Apply(MethodCallExpression operation);
    IQueryable Project();
}

internal class QueryShape<T>(IQueryable<T> source) : QueryShape<T, T>(source, x => x);

internal class QueryShape<T, Q>(IQueryable<Q> source, Expression<Func<Q, T>> shape) : IQueryShape
{
    public IQueryable<Q> Source { get; } = source;
    public Expression<Func<Q, T>> Shape { get; } = shape;

    IQueryable IQueryShape.Source => Source;
    LambdaExpression IQueryShape.Shape => Shape;

    public IQueryable Project() => Source.Select(Shape);

    public Expression<Func<Q, R>> Translate<R>(Expression<Func<T, R>> expression)
    {
        var body = new ParameterReplacingVisitor(expression.Parameters[0], Shape.Body)
            .Visit(expression.Body)!;
        body = new ProjectionSimplifyingVisitor().Visit(body)!;
        return Expression.Lambda<Func<Q, R>>(body, Shape.Parameters);
    }

    public IQueryShape Apply(MethodCallExpression operation)
    {
        if (operation.Method.DeclaringType != typeof(Queryable) ||
            operation.Method.Name is not (nameof(Queryable.Where) or nameof(Queryable.Select)))
            throw new NotSupportedException($"Unsupported query operation: {operation.Method.Name}.");

        var selector = (LambdaExpression)((UnaryExpression)operation.Arguments[1]).Operand;
        if (selector.Parameters.Count != 1)
            throw new NotSupportedException("Indexed query operators are not supported yet.");

        if (operation.Method.Name == nameof(Queryable.Where))
            return new QueryShape<T, Q>(
                Source.Where(Translate((Expression<Func<T, bool>>)selector)), Shape);

        if (operation.Method.Name == nameof(Queryable.Select))
            return (IQueryShape)typeof(QueryShape<T, Q>)
                .GetMethod(nameof(Select), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .MakeGenericMethod(selector.ReturnType)
                .Invoke(this, [selector])!;

        throw new NotSupportedException($"Unsupported query operation: {operation.Method.Name}.");
    }

    private IQueryShape Select<R>(Expression<Func<T, R>> selector)
        => new QueryShape<R, Q>(Source, Translate(selector));
}

