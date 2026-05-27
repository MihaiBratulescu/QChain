using System.Linq.Expressions;

namespace QChain.Internal.Visitors;

internal static class TupleExpressionNormalizer
{
    public static Expression Normalize(Expression expression)
    {
        expression = new ValueTupleCreateToCtorVisitor().Visit(expression)!;
        return new TupleAccessSimplifyingVisitor().Visit(expression)!;
    }
}
