using QChain.Internal.Helpers;
using QChain.Internal.Shapes;
using QChain.Visitors;
using System.Linq.Expressions;

namespace QChain;

internal interface IQueryShape
{
    IQueryable Source { get; }
    LambdaExpression Shape { get; }
    Type SourceType { get; }
    IQueryShape Apply(MethodCallExpression operation);
    IQueryable Project();
    Expression TranslateExecution(MethodCallExpression operation);
}

internal sealed class QueryShape<T>(IQueryable<T> source) : SequenceQueryShape<T, T>(source, x => x);

internal abstract class QueryShape<T, Q>(IQueryable<Q> source, Expression<Func<Q, T>> shape) : IQueryShape
{
    public IQueryable<Q> Source { get; } = source;
    public Expression<Func<Q, T>> Shape { get; } = shape;
    public Type SourceType => typeof(Q);

    IQueryable IQueryShape.Source => Source;
    LambdaExpression IQueryShape.Shape => Shape;

    public IQueryable<T> Project() => Source.Select(Shape);
    IQueryable IQueryShape.Project() => Project();

    public Expression<Func<Q, R>> Translate<R>(Expression<Func<T, R>> expression) =>
        (Expression<Func<Q, R>>)Translate((LambdaExpression)expression);

    private LambdaExpression Translate(LambdaExpression expression)
    {
        if (expression.Parameters.Count != 1)
            throw new NotSupportedException("Indexed query operators are not supported yet.");

        var body = new ParameterReplacingVisitor(expression.Parameters[0], Shape.Body).Visit(expression.Body)!;
        return Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(Q), expression.ReturnType),
            TupleExpressionNormalizer.Normalize(body), Shape.Parameters);
    }

    protected internal Expression<Func<C, T>> Rebuild<C>()
    {
        var row = Expression.Parameter(typeof(C), "row");
        return Expression.Lambda<Func<C, T>>(CarrierProjection.Rebuild(row, typeof(T)), row);
    }

    public abstract IQueryShape Apply(MethodCallExpression operation);

    public Expression TranslateExecution(MethodCallExpression operation)
    {
        var name = operation.Method.Name;
        var arguments = operation.Arguments.Skip(1).ToArray();
        var lambda = arguments.FirstOrDefault() is UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression selector }
            ? selector
            : null;

        if (name is nameof(Queryable.Any) or nameof(Queryable.All) or nameof(Queryable.Count) or nameof(Queryable.LongCount) ||
            (name is nameof(Queryable.Sum) or nameof(Queryable.Average) or nameof(Queryable.Min) or nameof(Queryable.Max) && lambda is not null))
        {
            if (lambda is not null)
                arguments[0] = Expression.Quote(Translate(lambda));

            var types = operation.Method.GetGenericArguments();
            if (types.Length > 0)
                types[0] = typeof(Q);

            var method = types.Length > 0 ? operation.Method.GetGenericMethodDefinition().MakeGenericMethod(types) : operation.Method;
            return Expression.Call(method, new[] { Source.Expression }.Concat(arguments));
        }

        if (lambda is not null && name is nameof(Queryable.First) or nameof(Queryable.FirstOrDefault) or
            nameof(Queryable.Last) or nameof(Queryable.LastOrDefault) or nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault))
        {
            var filtered = Source.Where((Expression<Func<Q, bool>>)Translate(lambda)).Select(Shape);
            return Expression.Call(typeof(Queryable), name, [typeof(T)], new[] { filtered.Expression }.Concat(arguments.Skip(1)).ToArray());
        }

        return operation.Update(operation.Object, new[] { Project().Expression }.Concat(arguments));
    }
}
