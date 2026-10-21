using QChain.Internal.Helpers;
using System.Linq.Expressions;

namespace QChain.Internal.Types;

internal interface IQueryShape
{
    IQueryable Source { get; }
    IQueryable Project();
}

internal sealed class QueryShape<T>(IQueryable<T> source) : IQueryShape
{
    public IQueryable Source => source;
    public IQueryable Project() => source;
}

internal sealed class QueryShape<T, Q>(IQueryable<Q> source) : IQueryShape
{
    private static readonly Expression<Func<Q, T>> Projection = CreateProjection();
    private static readonly Lazy<Func<Q, T>> ProjectItem = new(() => Projection.Compile());

    public IQueryable Source => source;
    public IQueryable Project() => source.Select(Projection);

    // Arrays have already been materialized by the provider at this public boundary.
    internal static T[] ProjectArray(Q[] items) => Array.ConvertAll(items, ProjectItem.Value.Invoke);

    private static Expression<Func<Q, T>> CreateProjection()
    {
        var item = Expression.Parameter(typeof(Q), "item");
        return Expression.Lambda<Func<Q, T>>(CarrierProjection.Rebuild(item, typeof(T)), item);
    }
}
