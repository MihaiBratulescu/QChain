using System.Linq.Expressions;

namespace QChain.Query;

internal interface IInternalQuery
{
    LambdaExpression UntypedShape { get; }
}
