namespace GreenDonut;

/// <summary>
/// Provides extension methods for <see cref="IPromiseCacheObserver"/>.
/// </summary>
public static class PromiseCacheObserverExtensions
{
    /// <summary>
    /// Accepts the cache of the <paramref name="dataLoader"/>
    /// </summary>
    /// <param name="observer">
    /// The <see cref="IPromiseCacheObserver"/> to accept the cache.
    /// </param>
    /// <param name="dataLoader">
    /// The <see cref="DataLoaderBase{TKey, TValue}"/> to accept the cache from.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the cache key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the cache value.
    /// </typeparam>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="observer"/> is <c>null</c>.
    /// </exception>
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
