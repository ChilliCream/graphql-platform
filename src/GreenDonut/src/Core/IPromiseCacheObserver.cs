namespace GreenDonut;

/// <summary>
/// The promise cache observer allows to subscribe to a
/// promise cache and create additional lookups for
/// already cached promises.
/// </summary>
public interface IPromiseCacheObserver : IDisposable
{
    /// <summary>
    /// Accepts the cache and subscribes to the cache.
    /// </summary>
    /// <param name="cache">
    /// The cache to subscribe to.
    /// </param>
    /// <param name="skipCacheKeyType">
    /// The cache key type of the owning <see cref="IDataLoader"/>.
    /// Items with this cache key type will be ignored when subscribing.
    /// </param>
    void Accept(IPromiseCache cache, string? skipCacheKeyType = null);
}

public static class PromiseCacheObserverExtensions
{
    public static void Accept<TKey, TValue>(
        this IPromiseCacheObserver observer,
        DataLoaderBase<TKey, TValue> dataLoader)
        where TKey : notnull
    {
        if (observer == null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        if (dataLoader == null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (dataLoader.Cache is not null)
        {
            observer.Accept(dataLoader.Cache, dataLoader.CacheKeyType);
        }
    }
}
