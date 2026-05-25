using QChain;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Visitors;

internal static class TupleProjection<T, Q>
{
    public static Expression Lower(Expression expression)
    {
        if (!TryGetValueTupleItems(expression.Type, out var itemTypes))
            return LowerObjectProjection(expression);

        var items = itemTypes
            .Select((_, index) => Lower(GetTupleItem(expression, index + 1)))
            .ToArray();

        return BuildProjectionTree(items);
    }

    public static Expression Rebuild(Expression expression, Type targetType)
    {
        if (!TryGetValueTupleItems(targetType, out var itemTypes))
            return RebuildObjectProjection(expression, targetType);

        var items = ReadProjectionTree(expression, itemTypes.Length)
            .Select((item, index) => Rebuild(item, itemTypes[index]))
            .ToArray();

        return Expression.New(targetType.GetConstructor(itemTypes)!, items);
    }

    private static Expression LowerObjectProjection(Expression expression)
    {
        if (expression is not NewExpression { Members: not null } ne ||
            ne.Arguments.Count < 2)
        {
            return expression;
        }

        var items = ne.Arguments.Select(Lower).ToArray();
        if (!items.Where((item, index) => item.Type != ne.Arguments[index].Type).Any())
            return expression;

        return BuildProjectionTree(items);
    }

    private static Expression RebuildObjectProjection(Expression expression, Type targetType)
    {
        if (!TryGetConstructorProjection(targetType, out var constructor, out var members) ||
            !IsProjectionType(expression.Type))
        {
            return expression;
        }

        var items = ReadProjectionTree(expression, members.Length)
            .Select((item, index) => Rebuild(item, GetMemberType(members[index])))
            .ToArray();

        return Expression.New(constructor, items, members);
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
        return typeof(Projection<,>).MakeGenericType(left, right);
    }

    private static bool IsProjectionType(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(Projection<,>);

    private static bool TryGetConstructorProjection(Type type, out ConstructorInfo constructor, out MemberInfo[] members)
    {
        constructor = null!;
        members = [];

        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetMethod is not null)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in type.GetConstructors())
        {
            var parameters = candidate.GetParameters();
            if (parameters.Length < 2)
                continue;

            var candidateMembers = new MemberInfo[parameters.Length];
            var matches = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                if (!properties.TryGetValue(parameters[i].Name!, out var property) ||
                    property.PropertyType != parameters[i].ParameterType)
                {
                    matches = false;
                    break;
                }

                candidateMembers[i] = property;
            }

            if (!matches)
                continue;

            constructor = candidate;
            members = candidateMembers;
            return true;
        }

        return false;
    }

    private static Type GetMemberType(MemberInfo member) =>
        member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new NotSupportedException($"Unsupported projection member '{member.Name}'.")
        };
}
