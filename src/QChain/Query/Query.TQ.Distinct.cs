using QChain.Internal;
using System.Linq.Expressions;

namespace QChain;

public partial class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Distinct() =>
        new Query<T, T>(Source.Select(Shape).Distinct(), x => x);

    public IQuery<R> DistinctBy<R>(Expression<Func<T, R>> selector) =>
        new Query<R, R>(Source.Select(Compose(selector, Shape)).Distinct(), x => x);
}
