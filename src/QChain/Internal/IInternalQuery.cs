using System.Linq.Expressions;

namespace QChain.Internal;

internal interface IInternalQuery
{
    LambdaExpression UntypedShape { get; }
}
