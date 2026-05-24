using QChain.Internal;
using System.Linq.Expressions;

namespace QChain.Visitors;

internal static class TupleProjection<T, Q>
{
    public static Expression Lower(Expression expression)
    {
        if (!TryGetValueTupleItems(expression.Type, out var itemTypes))
            return expression;

        var items = itemTypes
            .Select((_, index) => Lower(GetTupleItem(expression, index + 1)))
            .ToArray();

        return BuildProjectionTree(items);
    }

    public static Expression Rebuild(Expression expression, Type targetType)
    {
        if (!TryGetValueTupleItems(targetType, out var itemTypes))
            return expression;

        var items = ReadProjectionTree(expression, itemTypes.Length)
            .Select((item, index) => Rebuild(item, itemTypes[index]))
            .ToArray();

        return Expression.New(targetType.GetConstructor(itemTypes)!, items);
    }

    private static Expression GetTupleItem(Expression tuple, int item)
    {
        if (!ProjectionReduction.TryRewriteTupleAccess(tuple, $"Item{item}", out var value))
            value = Expression.PropertyOrField(tuple, $"Item{item}");

        return value;
    }

    private static bool TryGetValueTupleItems(Type type, out Type[] items)
    {
        if (type.IsGenericType &&
            type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
        {
            var args = type.GetGenericArguments();
            if (args.Length > 7)
                throw new NotSupportedException("ValueTuple arity > 7 not supported yet.");

            items = args;
            return true;
        }

        items = [];
        return false;
    }

    private static Expression BuildProjectionTree(IReadOnlyList<Expression> items)
    {
        if (items.Count == 2)
            return CreateProjection(items[0], items[1]);

        return CreateProjection(
            BuildProjectionTree(items.Take(items.Count - 1).ToArray()),
            items[^1]);
    }

    private static Expression[] ReadProjectionTree(Expression projection, int count)
    {
        if (count == 2)
        {
            return
            [
                Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item1)),
                    Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item2))
            ];
        }

        var left = Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item1));
        var right = Expression.PropertyOrField(projection, nameof(Projection<int, int>.Item2));

        return [.. ReadProjectionTree(left, count - 1), right];
    }

    private static Expression CreateProjection(Expression left, Expression right)
    {
        var projectionType = MakeProjectionType(left.Type, right.Type);

        return Expression.MemberInit(
            Expression.New(projectionType),
            Expression.Bind(projectionType.GetProperty(nameof(Projection<int, int>.Item1))!, left),
            Expression.Bind(projectionType.GetProperty(nameof(Projection<int, int>.Item2))!, right));
    }

    private static Type MakeProjectionType(Type left, Type right)
    {
        var definition = typeof(Projection<int, int>).GetGenericTypeDefinition();
        var arguments = definition.GetGenericArguments().Length == 2
            ? [left, right]
            : new[] { typeof(T), typeof(Q), left, right };

        return definition.MakeGenericType(arguments);
    }
}