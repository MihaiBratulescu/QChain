using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace QChain.EntityFrameworkCore;

public class EntityQuery<T> : Query<T, T>, IEntityQuery<T>
    where T : class
{
    public EntityQuery(IQueryable<T> query) : base(query, q => q) { }

    public IEntityQuery<T> AsNoTracking() =>
        new EntityQuery<T>(Source.AsNoTracking());

    public IEntityQuery<T> Include<N>(Expression<Func<T, N>> include) =>
        new EntityQuery<T>(Source.Include(include));
}
