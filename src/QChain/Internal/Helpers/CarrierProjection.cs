using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace QChain.Internal.Helpers;

internal static class CarrierProjection
{
    public static bool IsTuple(Type type) => type.IsGenericType &&
        type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    public static bool IsCarrier(Type type) => type.IsGenericType &&
        Definitions.Contains(type.GetGenericTypeDefinition());

    public static Type RewriteType(Type type)
    {
        if (type.IsArray)
        {
            var element = RewriteType(type.GetElementType()!);
            return element == type.GetElementType() ? type :
                type.IsSZArray ? element.MakeArrayType() : element.MakeArrayType(type.GetArrayRank());
        }

        if (!type.IsGenericType)
            return type;

        if (Nullable.GetUnderlyingType(type) is { } underlying && IsTuple(underlying))
            return typeof(DefaultIfEmptyValue<>).MakeGenericType(RewriteType(underlying));

        var arguments = type.GetGenericArguments();
        var rewritten = arguments.Select(RewriteType).ToArray();
        if (IsTuple(type))
        {
            if (arguments.Length > 7)
                throw new NotSupportedException("ValueTuple arity > 7 is not supported yet.");
            return Definitions[arguments.Length - 1].MakeGenericType(rewritten);
        }

        return arguments.SequenceEqual(rewritten) ? type : type.GetGenericTypeDefinition().MakeGenericType(rewritten);
    }

    public static MemberInitExpression Create(IReadOnlyList<Expression> items)
    {
        if (items.Count is < 1 or > 7)
            throw new NotSupportedException("Carrier projections support between one and seven items.");

        var type = Definitions[items.Count - 1].MakeGenericType(items.Select(item => item.Type).ToArray());
        return Expression.MemberInit(Expression.New(type),
            items.Select((item, index) => Expression.Bind(type.GetProperty($"Item{index + 1}")!, item)));
    }

    public static Expression Lower(Expression expression) => Convert(expression, RewriteType(expression.Type));
    public static Expression Rebuild(Expression expression, Type targetType) => Convert(expression, targetType);

    public static MemberInitExpression NullableValue(Expression value)
    {
        var type = typeof(DefaultIfEmptyValue<>).MakeGenericType(value.Type);
        return Expression.MemberInit(Expression.New(type),
            Expression.Bind(type.GetProperty("HasValue")!, Expression.Constant(true)),
            Expression.Bind(type.GetProperty("Value")!, value));
    }

    // Conversion is needed only at public boundaries: roots, captured values, and final results.
    private static Expression Convert(Expression expression, Type targetType)
    {
        if (expression.Type == targetType || targetType.IsAssignableFrom(expression.Type))
            return expression;

        if (Nullable.GetUnderlyingType(targetType) is { } targetTuple && IsTuple(targetTuple))
            return Expression.Condition(Expression.Property(expression, "HasValue"),
                Expression.Convert(Convert(Expression.Property(expression, "Value"), targetTuple), targetType), Expression.Default(targetType));

        if (Nullable.GetUnderlyingType(expression.Type) is { } sourceTuple && IsTuple(sourceTuple))
        {
            var hasValue = Expression.Property(expression, "HasValue");
            var value = Lower(Expression.Property(expression, "Value"));
            return Expression.MemberInit(Expression.New(targetType),
                Expression.Bind(targetType.GetProperty("HasValue")!, hasValue),
                Expression.Bind(targetType.GetProperty("Value")!, Expression.Condition(hasValue, value, Expression.Default(value.Type))));
        }

        if (IsTuple(targetType) || IsCarrier(targetType))
        {
            var types = targetType.GetGenericArguments();
            var items = types.Select((type, index) =>
                Convert(Expression.PropertyOrField(expression, $"Item{index + 1}"), type)).ToArray();
            return IsTuple(targetType)
                ? Expression.New(targetType.GetConstructor(types)!, items)
                : Create(items);
        }

        if (IsAnonymous(targetType))
        {
            var constructor = targetType.GetConstructors().Single();
            var members = constructor.GetParameters().Select(parameter => targetType.GetProperty(parameter.Name!)!).ToArray();
            return Expression.New(constructor,
                members.Select(member => Convert(Expression.Property(expression, member.Name), member.PropertyType)), members);
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            var types = targetType.GetGenericArguments();
            var items = ConvertSequence(expression, typeof(IEnumerable<>).MakeGenericType(types[1]));
            return Expression.New(typeof(ProjectedGrouping<,>).MakeGenericType(types).GetConstructors().Single(),
                Convert(Expression.Property(expression, "Key"), types[0]), items);
        }

        if (SequenceElement(targetType) is not null)
            return ConvertSequence(expression, targetType);

        throw new NotSupportedException($"Cannot substitute tuples in '{targetType}'.");
    }

    private static Expression ConvertSequence(Expression expression, Type targetType)
    {
        var sourceElement = SequenceElement(expression.Type)!;
        var targetElement = SequenceElement(targetType)!;
        var item = Expression.Parameter(sourceElement, "item");
        var selector = Expression.Lambda(Convert(item, targetElement), item);
        var queryable = typeof(IQueryable).IsAssignableFrom(targetType);
        Expression result = Expression.Call(queryable ? typeof(Queryable) : typeof(Enumerable), nameof(Enumerable.Select),
            [sourceElement, targetElement], expression, queryable ? Expression.Quote(selector) : selector);

        if (targetType.IsArray || targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            result = Expression.Call(typeof(Enumerable), targetType.IsArray ? nameof(Enumerable.ToArray) : nameof(Enumerable.ToList),
                [targetElement], result);

        if (!targetType.IsAssignableFrom(result.Type))
            throw new NotSupportedException($"Cannot substitute tuples in sequence '{targetType}'.");
        return result;
    }

    public static Type? SequenceElement(Type type) => type.GetInterfaces().Prepend(type)
        .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        ?.GetGenericArguments()[0];

    public static bool IsAnonymous(Type type) => type.IsDefined(typeof(CompilerGeneratedAttribute), false) &&
        type.Name.Contains("AnonymousType", StringComparison.Ordinal);

    private sealed class ProjectedGrouping<K, T>(K key, IEnumerable<T> items) : IGrouping<K, T>
    {
        public K Key => key;
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static readonly Type[] Definitions =
    [
        typeof(Carrier<>), typeof(Carrier<,>), typeof(Carrier<,,>), typeof(Carrier<,,,>),
        typeof(Carrier<,,,,>), typeof(Carrier<,,,,,>), typeof(Carrier<,,,,,,>)
    ];
}
