namespace GreenDonut;

/// <summary>
/// Allows to implement a second-level cache for the DataLoader promise cache.
/// </summary>
public interface IPromiseCacheInterceptor
{
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
    Promise<T> GetOrAddPromise<T>(PromiseCacheKey key, Func<PromiseCacheKey, Promise<T>> createPromise);

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
}
