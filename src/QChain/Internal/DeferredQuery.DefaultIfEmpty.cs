using QChain.Visitors;
using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T?> DefaultIfEmpty() =>
        new Query<T?>(Source.Select(Shape).DefaultIfEmpty());

    public IQuery<T> DefaultIfEmpty(T value) =>
        new Query<T>(Source.Select(Shape).DefaultIfEmpty(value));

    private static Expression<Func<TSource?, TResult?>> NullableTranslate<TSource, TResult>(
        Expression<Func<TSource, TResult>> expression)
    {
        var parameter = Expression.Parameter(typeof(TSource), expression.Parameters[0].Name);

        var body = new ReplaceExpressionVisitor(
            expression.Parameters[0],
            parameter)
            .Visit(expression.Body)!;

        return Expression.Lambda<Func<TSource?, TResult?>>(body, parameter);
    }
}
