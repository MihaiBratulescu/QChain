using QChain.Internal;
using QChain.Predicates;
using System.Linq.Expressions;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new Query<T, Q>(Source.Where(Translate(predicate)), Shape);

    public IQuery<T> Where(Func<T, Predicate> predicate) => 
        Where(PredicateCompiler.Compile(predicate));
}
