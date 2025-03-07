namespace GreenDonut;

/// <summary>
/// A memorization cache for <c>DataLoader</c>.
/// </summary>
public interface IPromiseCache
{
    /// <summary>
    /// Gets the maximum size of the cache.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets the count of the entries inside the cache.
    /// </summary>
    int Usage { get; }

    /// <summary>
    /// Gets a task from the cache if a task with the specified <paramref name="key"/> already
    /// exists; otherwise, the <paramref name="createPromise"/> factory is used to create a new
    /// task and add it to the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <param name="createPromise">A factory to create the new task.</param>
    /// <typeparam name="T">The task type.</typeparam>
    /// <returns>
    /// Returns either the retrieved or new task from the cache.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="createPromise"/> is <c>null</c>.
    /// </exception>
    Task<T> GetOrAddTask<T>(PromiseCacheKey key, Func<PromiseCacheKey, Promise<T>> createPromise);

    /// <summary>
    /// Tries to add a single task to the cache. It does nothing if the
    /// task exists already.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <param name="promise">A task.</param>
    /// <typeparam name="T">The task type.</typeparam>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="promise"/> is <c>null</c>.
    /// </exception>
    /// <returns>
    /// A value indicating whether the add was successful.
    /// </returns>
    bool TryAdd<T>(PromiseCacheKey key, Promise<T> promise);

    /// <summary>
    /// Tries to add a single task to the cache. It does nothing if the
    /// task exists already.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <param name="createPromise">A factory to create the new task.</param>
    /// <typeparam name="T">The task type.</typeparam>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="createPromise"/> is <c>null</c>.
    /// </exception>
    /// <returns>
    /// A value indicating whether the add was successful.
    /// </returns>
    bool TryAdd<T>(PromiseCacheKey key, Func<Promise<T>> createPromise);

    /// <summary>
    /// Removes a specific task from the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    bool TryRemove(PromiseCacheKey key);

    /// <summary>
    /// Publishes a value to the cache subscribers without adding it to the cache  iself.
    /// This allows the subscribers to decide if they want to cache the value.
    /// </summary>
    /// <param name="value">
    /// The value that is published to the subscribers.
    /// </param>
    /// <typeparam name="T">
    /// The value type.
    /// </typeparam>
    void Publish<T>(T value);

    /// <summary>
    /// Publishes the values to the cache subscribers without adding it to the cache iself.
    /// This allows the subscribers to decide if they want to cache the values.
    /// </summary>
    /// <param name="values">
    /// The values that are published to the subscribers.
    /// </param>
    /// <typeparam name="T">
    /// The value type.
    /// </typeparam>
    void PublishMany<T>(ReadOnlySpan<T> values);

    /// <summary>
    /// Subscribes to the cache and gets notified when a new task is added.
    /// </summary>
    /// <param name="next">
    /// The action that is called when a new task is added.
    /// </param>
    /// <param name="skipCacheKeyType">
    /// The cache key type that should be skipped.
    /// </param>
    /// <typeparam name="T">
    /// The task type.
    /// </typeparam>
    /// <returns>
    /// Returns a disposable that can be used to unsubscribe.
    /// </returns>
    IDisposable Subscribe<T>(
        Action<IPromiseCache, Promise<T>> next,
        string? skipCacheKeyType);

    /// <summary>
    /// Clears the complete cache.
    /// </summary>
    void Clear();
}
