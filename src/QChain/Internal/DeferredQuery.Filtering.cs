using QChain.Predicates;
using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new DeferredQuery<T, Q>(Source.Where(Translate(predicate)), Shape);

    public IQuery<T> Where(Func<T, Predicate> predicate)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var tree = predicate(default(T)!);

        var body = PredicateCompiler.Compile(tree, parameter);

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

        return Where(lambda);
    }


    public IQuery<T> Distinct() =>
        new DeferredQuery<T, T>(Source.Select(Shape).Distinct(), x => x);

    public IQuery<R> DistinctBy<R>(Expression<Func<T, R>> selector) =>
        new DeferredQuery<R, R>(Source.Select(Compose(selector, Shape)).Distinct(), x => x);
}
