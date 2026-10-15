using System.Linq.Expressions;

namespace QChain.Visitors;

internal sealed class ParameterReplacingVisitor(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == parameter ? replacement : base.VisitParameter(node);
}
