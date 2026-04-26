using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class ValueTupleCreateToCtorVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var visited = (MethodCallExpression)base.VisitMethodCall(node);

        if (visited.Method.DeclaringType != typeof(ValueTuple) ||
            visited.Method.Name != nameof(ValueTuple.Create))
            return visited;

        var args = visited.Arguments;
        var types = args.Select(a => a.Type).ToArray();

        var tupleType = types.Length switch
        {
            1 => typeof(ValueTuple<>).MakeGenericType(types),
            2 => typeof(ValueTuple<,>).MakeGenericType(types),
            3 => typeof(ValueTuple<,,>).MakeGenericType(types),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(types),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(types),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(types),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(types),
            8 => typeof(ValueTuple<,,,,,,,>).MakeGenericType(types),
            _ => throw new NotSupportedException("ValueTuple arity > 8 not supported yet.")
        };

        var ctor = tupleType.GetConstructor(types)!;
        return Expression.New(ctor, args);
    }
}