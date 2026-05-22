using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Distinct() =>
        new DeferredQuery<T, T>(Source.Select(Shape).Distinct(), x => x);

    public IQuery<R> DistinctBy<R>(Expression<Func<T, R>> selector) =>
        new DeferredQuery<R, R>(Source.Select(Compose(selector, Shape)).Distinct(), x => x);
}
