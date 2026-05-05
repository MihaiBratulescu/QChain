using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

internal interface IInternalQuery
{
    LambdaExpression UntypedShape { get; }
}
