using System.Linq.Expressions;

namespace QChain;

internal interface IInternalQuery
{
    LambdaExpression UntypedShape { get; }
}
