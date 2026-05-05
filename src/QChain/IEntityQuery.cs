using System.Linq.Expressions;

namespace QChain;

public interface IEntityQuery<T> : IQuery<T> where T : class
{
    IEntityQuery<T> AsNoTracking();
    IEntityQuery<T> Include<N>(Expression<Func<T, N>> include);

    IEntityQuery<T> AsSplitQuery();
    IEntityQuery<T> AsSingleQuery();
}
