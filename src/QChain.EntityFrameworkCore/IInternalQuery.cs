using System.Linq.Expressions;

namespace QChain.EntityFrameworkCore;

internal interface IInternalQuery
{
    IQueryable UntypedSource { get; }
    LambdaExpression UntypedShape { get; }
}
