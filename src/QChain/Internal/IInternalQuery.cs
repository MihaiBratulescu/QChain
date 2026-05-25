using System.Linq.Expressions;

namespace QChain.Internal;

internal interface IInternalQuery
{
    IQueryable UntypedSource { get; }
    LambdaExpression UntypedShape { get; }
}
