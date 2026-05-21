namespace QChain;

public static class IQueryExtensions
{
    extension<T>(IQuery<T> source) where T : class
    {
        public IQuery<T> DefaultIfEmpty(T value) => new Query<T>(
            source.AsQueryable().DefaultIfEmpty().Select(x => x ?? value));
    }
}