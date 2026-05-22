using QChain.Predicates;
using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new DeferredQuery<T, Q>(Source.Where(Translate(predicate)), Shape);

    public IQuery<T> Where(Func<T, Predicate> predicate) => 
        Where(PredicateCompiler.Compile(predicate));
}
