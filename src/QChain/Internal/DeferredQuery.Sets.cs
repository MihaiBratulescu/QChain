using System.Linq.Expressions;

namespace QChain.Internal;

public partial class DeferredQuery<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    public IQuery<T> Union(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Union((other as DeferredQuery<T, Q>)!.Source), Shape);
    public IQuery<T> UnionBy<K>(IQuery<T> other, Expression<Func<T, K>> key) =>
        new DeferredQuery<T, Q>(Source.UnionBy((other as DeferredQuery<T, Q>)!.Source, Translate(key)), Shape);

    public IQuery<T> Concat(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Concat((other as DeferredQuery<T, Q>)!.Source), Shape);

    public IQuery<T> Except(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Except((other as DeferredQuery<T, Q>)!.Source), Shape);
    public IQuery<T> ExceptBy<K>(IQuery<T> other, Expression<Func<T, K>> key) =>
        new DeferredQuery<T, Q>(Source.ExceptBy((other as DeferredQuery<T, Q>)!.Source.Select(Translate(key)), Translate(key)), Shape);

    public IQuery<T> Intersect(IQuery<T> other) => new DeferredQuery<T, Q>(Source.Intersect((other as DeferredQuery<T, Q>)!.Source), Shape);
    public IQuery<T> IntersectBy<K>(IQuery<T> other, Expression<Func<T, K>> key) =>
        new DeferredQuery<T, Q>(Source.IntersectBy((other as DeferredQuery<T, Q>)!.Source.Select(Translate(key)), Translate(key)), Shape);
}