using System.Linq.Expressions;

namespace QChain.Visitors;

internal sealed class ProjectionSimplifyingVisitor : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        var target = Visit(node.Expression);

        if (target is NewExpression { Members: not null } projection)
        {
            var index = projection.Members.IndexOf(node.Member);
            if (index >= 0)
                return projection.Arguments[index];
        }

        if (node.Member.DeclaringType?.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true &&
            node.Member.Name.StartsWith("Item", StringComparison.Ordinal) &&
            int.TryParse(node.Member.Name.AsSpan(4), out var item))
        {
            if (target is NewExpression tuple && item <= tuple.Arguments.Count)
                return tuple.Arguments[item - 1];

        }

        return node.Update(target);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(ValueTuple) && node.Method.Name == nameof(ValueTuple.Create) &&
            node.Arguments.Count is > 0 and <= 7)
            return Expression.New(node.Type.GetConstructor(node.Method.GetGenericArguments())!,
                node.Arguments.Select(argument => Visit(argument)!));

        return base.VisitMethodCall(node);
    }
}
