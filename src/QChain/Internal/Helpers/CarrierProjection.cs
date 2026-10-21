using System.Linq.Expressions;
using System.Reflection;
using QChain.Visitors;

namespace QChain.Internal.Helpers;

internal static class CarrierProjection
{
    public static (LambdaExpression Pack, LambdaExpression Shape) Combine(
        LambdaExpression leftShape, LambdaExpression rightShape, LambdaExpression result)
    {
        var left = Expression.Parameter(leftShape.Parameters[0].Type, "left");
        var right = Expression.Parameter(rightShape.Parameters[0].Type, "right");
        var leftItems = Items(left);
        var rightItems = Items(right);
        // Wider joins keep nested carriers so each carrier stays within seven items.
        var flatten = leftItems.Length + rightItems.Length <= 7;
        var packed = Create(flatten ? [.. leftItems, .. rightItems] : [left, right]);
        var row = Expression.Parameter(packed.Type, "row");
        var items = Read(row, packed.Type.GetGenericArguments().Length);
        var leftRow = flatten && IsCarrier(left.Type) ? Create(items.Take(leftItems.Length).ToArray()) : items[0];
        var rightRow = flatten && IsCarrier(right.Type)
            ? Create(items.Skip(leftItems.Length).ToArray())
            : items[flatten ? leftItems.Length : 1];
        var leftPublic = new ParameterReplacingVisitor(leftShape.Parameters[0], leftRow).Visit(leftShape.Body)!;
        var rightPublic = new ParameterReplacingVisitor(rightShape.Parameters[0], rightRow).Visit(rightShape.Body)!;
        var body = new ParameterReplacingVisitor(result.Parameters[0], leftPublic).Visit(result.Body)!;
        body = new ParameterReplacingVisitor(result.Parameters[1], rightPublic).Visit(body)!;

        return (Expression.Lambda(packed, left, right),
            Expression.Lambda(TupleExpressionNormalizer.Normalize(body), row));
    }

    private static Expression[] Items(Expression row) => IsCarrier(row.Type)
        ? Read(row, row.Type.GetGenericArguments().Length)
        : [row];

    public static Expression Lower(Expression expression)
    {
        if (!TryGetValueTupleItems(expression.Type, out var itemTypes))
            return LowerObjectProjection(expression);

        var items = itemTypes.Select((_, index) => Lower(GetTupleItem(expression, index + 1))).ToArray();
        return Create(items);
    }

    public static Expression Rebuild(Expression expression, Type targetType)
    {
        if (expression.Type == targetType)
            return expression;

        if (!TryGetValueTupleItems(targetType, out var itemTypes))
            return RebuildObjectProjection(expression, targetType);

        var items = Read(expression, itemTypes.Length)
            .Select((item, index) => Rebuild(item, itemTypes[index])).ToArray();
        return Expression.New(targetType.GetConstructor(itemTypes)!, items);
    }

    public static bool IsCarrier(Type type) => type.IsGenericType &&
        Definitions.Contains(type.GetGenericTypeDefinition());

    public static MemberInitExpression Create(IReadOnlyList<Expression> items)
    {
        if (items.Count is < 1 or > 7)
            throw new NotSupportedException("Carrier projections support between one and seven items.");

        var type = Definitions[items.Count - 1].MakeGenericType(items.Select(item => item.Type).ToArray());
        return Expression.MemberInit(Expression.New(type),
            items.Select((item, index) => Expression.Bind(type.GetProperty($"Item{index + 1}")!, item)));
    }

    private static Expression LowerObjectProjection(Expression expression)
    {
        if (expression is not NewExpression { Members: not null } projection || projection.Arguments.Count == 0)
            return expression;

        var items = projection.Arguments.Select(Lower).ToArray();
        return items.Where((item, index) => item.Type != projection.Arguments[index].Type).Any()
            ? Create(items)
            : expression;
    }

    private static Expression RebuildObjectProjection(Expression expression, Type targetType)
    {
        if (!IsCarrier(expression.Type))
            return expression;

        var properties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetMethod is not null)
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var constructor in targetType.GetConstructors())
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0 || parameters.Length != expression.Type.GetGenericArguments().Length)
                continue;

            if (!parameters.All(parameter => properties.TryGetValue(parameter.Name!, out var property) &&
                    property.PropertyType == parameter.ParameterType))
                continue;

            var members = parameters.Select(parameter => properties[parameter.Name!]).ToArray();
            var items = Read(expression, members.Length)
                .Select((item, index) => Rebuild(item, members[index].PropertyType));
            return Expression.New(constructor, items, members);
        }

        throw new NotSupportedException($"Cannot rebuild projection '{targetType}'.");
    }

    private static Expression[] Read(Expression expression, int count) =>
        Enumerable.Range(1, count).Select(index => Expression.PropertyOrField(expression, $"Item{index}")).ToArray();

    private static Expression GetTupleItem(Expression tuple, int item) =>
        ProjectionReduction.TryRewriteTupleAccess(tuple, $"Item{item}", out var value)
            ? value
            : Expression.PropertyOrField(tuple, $"Item{item}");

    private static bool TryGetValueTupleItems(Type type, out Type[] items)
    {
        if (type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
        {
            items = type.GetGenericArguments();
            if (items.Length > 7)
                throw new NotSupportedException("ValueTuple arity > 7 is not supported yet.");
            return true;
        }

        items = [];
        return false;
    }

    private static readonly Type[] Definitions =
    [
        typeof(Carrier<>), typeof(Carrier<,>), typeof(Carrier<,,>), typeof(Carrier<,,,>),
        typeof(Carrier<,,,,>), typeof(Carrier<,,,,,>), typeof(Carrier<,,,,,,>)
    ];
}
