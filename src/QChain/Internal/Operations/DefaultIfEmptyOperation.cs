using QChain.Internal.Helpers;
using QChain.Internal.Shapes;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable CS8620

namespace QChain.Internal.Operations;

internal static class DefaultIfEmptyOperation
{
    public static IQueryable<T?> Apply<T, Q>(SequenceQueryShape<T, Q> shape)
    {
        if (TryGetNullableTupleValue(shape.Shape.Body, out var tupleType, out var tupleValue))
            return ApplyNullableTuple<T, Q>(shape.Source, shape.Shape.Parameters, tupleType, tupleValue);

        var lowered = TupleProjection<T, Q>.Lower(shape.Shape.Body);
        if (lowered.Type == typeof(T))
            return shape.Project().DefaultIfEmpty();

        return ApplyTuple<T, Q>(shape.Source, shape.Shape.Parameters, lowered);
    }

    private static IQueryable<T?> ApplyTuple<T, Q>(
        IQueryable<Q> source,
        IReadOnlyCollection<ParameterExpression> parameters,
        Expression lowered)
    {
        var selector = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(Q), lowered.Type),
            lowered,
            parameters);

        return (IQueryable<T?>)ApplyTupleMethod
            .MakeGenericMethod(typeof(T), typeof(Q), lowered.Type)
            .Invoke(null, [source, selector])!;
    }

    private static IQueryable<T?> ApplyTupleCore<T, Q, C>(
        IQueryable<Q> source,
        Expression<Func<Q, C>> lowered)
    {
        var row = Expression.Parameter(typeof(DefaultIfEmptyValue<C>), "x");
        var carrier = Expression.Property(row, nameof(DefaultIfEmptyValue<C>.Value));
        var hasValue = Expression.Property(row, nameof(DefaultIfEmptyValue<C>.HasValue));
        var defaulted = DefaultCarrier(carrier, hasValue);
        var body = TupleProjection<T, C>.Rebuild(defaulted, typeof(T));
        var selector = Expression.Lambda<Func<DefaultIfEmptyValue<C>, T?>>(body, row);

        return source
            .Select(lowered)
            .Select(x => new DefaultIfEmptyValue<C> { HasValue = true, Value = x })
            .DefaultIfEmpty()
            .Select(selector);
    }

    private static IQueryable<T?> ApplyNullableTuple<T, Q>(
        IQueryable<Q> source,
        IReadOnlyCollection<ParameterExpression> parameters,
        Type tupleType,
        Expression tupleValue)
    {
        var lowered = (Expression)LowerTupleMethod
            .MakeGenericMethod(tupleType, typeof(Q))
            .Invoke(null, [tupleValue])!;
        var selector = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(Q), lowered.Type),
            lowered,
            parameters);

        return (IQueryable<T?>)ApplyNullableTupleMethod
            .MakeGenericMethod(typeof(T), typeof(Q), tupleType, lowered.Type)
            .Invoke(null, [source, selector])!;
    }

    private static IQueryable<T?> ApplyNullableTupleCore<T, Q, TTuple, C>(
        IQueryable<Q> source,
        Expression<Func<Q, C>> lowered)
        where TTuple : struct
    {
        var row = Expression.Parameter(typeof(DefaultIfEmptyValue<C>), "x");
        var carrier = Expression.Property(row, nameof(DefaultIfEmptyValue<C>.Value));
        var hasValue = Expression.Property(row, nameof(DefaultIfEmptyValue<C>.HasValue));
        var defaulted = DefaultCarrier(carrier, hasValue);
        var tuple = TupleProjection<TTuple, C>.Rebuild(defaulted, typeof(TTuple));
        var body = Expression.Condition(
            hasValue,
            Expression.New(typeof(T).GetConstructor([typeof(TTuple)])!, tuple),
            Expression.Default(typeof(T)));
        var selector = Expression.Lambda<Func<DefaultIfEmptyValue<C>, T?>>(body, row);

        return source
            .Select(lowered)
            .Select(x => new DefaultIfEmptyValue<C> { HasValue = true, Value = x })
            .DefaultIfEmpty()
            .Select(selector);
    }

    private static Expression DefaultCarrier(Expression carrier, Expression hasValue)
    {
        if (!TryGetCarrierItems(carrier.Type, out var itemTypes, out var members))
            return Expression.Condition(hasValue, carrier, Expression.Default(carrier.Type));

        var items = itemTypes
            .Select((_, index) => DefaultCarrier(Expression.PropertyOrField(carrier, members[index]), hasValue))
            .ToArray();

        return CreateCarrier(carrier.Type, itemTypes, members, items);
    }

    private static Expression CreateCarrier(Type type, Type[] itemTypes, string[] members, Expression[] items)
    {
        if (IsValueTuple(type))
            return Expression.New(type.GetConstructor(itemTypes)!, items);

        return Expression.MemberInit(
            Expression.New(type),
            members.Select((member, index) => Expression.Bind(type.GetProperty(member)!, items[index])));
    }

    private static bool TryGetCarrierItems(Type type, out Type[] itemTypes, out string[] members)
    {
        if (IsValueTuple(type))
        {
            itemTypes = type.GetGenericArguments();
            members = itemTypes.Select((_, index) => $"Item{index + 1}").ToArray();
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Projection<,>))
        {
            itemTypes = type.GetGenericArguments();
            members = [nameof(Projection<int, int>.Item1), nameof(Projection<int, int>.Item2)];
            return true;
        }

        itemTypes = [];
        members = [];
        return false;
    }

    private static bool TryGetNullableTupleValue(Expression expression, out Type tupleType, out Expression value)
    {
        tupleType = Nullable.GetUnderlyingType(expression.Type)!;
        if (tupleType is null || !IsValueTuple(tupleType))
        {
            value = null!;
            return false;
        }

        value = expression switch
        {
            NewExpression { Arguments.Count: 1 } ne => ne.Arguments[0],
            UnaryExpression { NodeType: ExpressionType.Convert } unary => unary.Operand,
            _ => null!
        };

        return value is not null;
    }

    private static Expression LowerTuple<TTuple, Q>(Expression expression) =>
        TupleProjection<TTuple, Q>.Lower(expression);

    private static bool IsValueTuple(Type type) =>
        type.IsGenericType &&
        type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    private static readonly MethodInfo ApplyTupleMethod =
        typeof(DefaultIfEmptyOperation).GetMethod(nameof(ApplyTupleCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ApplyNullableTupleMethod =
        typeof(DefaultIfEmptyOperation).GetMethod(nameof(ApplyNullableTupleCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo LowerTupleMethod =
        typeof(DefaultIfEmptyOperation).GetMethod(nameof(LowerTuple), BindingFlags.NonPublic | BindingFlags.Static)!;
}
