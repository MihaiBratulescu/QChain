using QChain.Internal.Visitors;
using System.Linq.Expressions;

namespace QChain.Internal.Helpers;

internal static class TupleExpressionNormalizer
{
    public static Expression Normalize(Expression expression)
    {
        expression = new ValueTupleCreateToCtorVisitor().Visit(expression)!;
        expression = new NullableMemberSimplifyingVisitor().Visit(expression)!;
        expression = new TupleAccessSimplifyingVisitor().Visit(expression)!;
        return new NullableNullComparisonSimplifyingVisitor().Visit(expression)!;
    }
}
