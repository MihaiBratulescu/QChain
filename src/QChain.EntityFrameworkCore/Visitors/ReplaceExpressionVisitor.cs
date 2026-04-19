using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly IReadOnlyDictionary<Expression, Expression> _map;

    public ReplaceExpressionVisitor(Expression from, Expression to)
        : this(new Dictionary<Expression, Expression>(ReferenceEqualityComparer.Instance) { [from] = to })
    {
    }

    public ReplaceExpressionVisitor(IReadOnlyDictionary<Expression, Expression> map)
    {
        if (map.Count == 0)
            throw new ArgumentException("Replacement map cannot be empty.", nameof(map));

        var normalized = new Dictionary<Expression, Expression>(map.Count, ReferenceEqualityComparer.Instance);
        foreach (var pair in map)
            normalized[pair.Key] = pair.Value;

        _map = new ReadOnlyDictionary<Expression, Expression>(normalized);
    }

    public static Expression Replace(Expression body, Expression from, Expression to) =>
        new ReplaceExpressionVisitor(from, to).Visit(body)!;

    public static Expression ReplaceMany(Expression body, IReadOnlyDictionary<Expression, Expression> map) =>
        new ReplaceExpressionVisitor(map).Visit(body)!;

    public override Expression? Visit(Expression? node)
    {
        if (node is not null && _map.TryGetValue(node, out var replacement))
            return replacement;

        return base.Visit(node);
    }
}
