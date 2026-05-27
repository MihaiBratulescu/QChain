using System.Linq.Expressions;

namespace QChain.Internal;

internal interface IQueryShape
{
    IQueryable UntypedSource { get; }
    LambdaExpression UntypedShape { get; }
    Type SourceType { get; }
}
