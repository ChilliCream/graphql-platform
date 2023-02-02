using System;
using System.Threading.Tasks;

namespace GreenDonut;

/// <summary>
/// A memorization cache for <c>DataLoader</c>.
/// </summary>
public interface ITaskCache
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
    /// exists; otherwise, the <paramref name="createTask"/> factory is used to create a new
    /// task and add it to the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <param name="createTask">A factory to create the new task.</param>
    /// <typeparam name="T">The task type.</typeparam>
    /// <returns>
    /// Returns either the retrieved or new task from the cache.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="createTask"/> is <c>null</c>.
    /// </exception>
    T GetOrAddTask<T>(TaskCacheKey key, Func<T> createTask) where T : Task;

    /// <summary>
    /// Tries to add a single task to the cache. It does nothing if the
    /// task exists already.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <param name="value">A task.</param>
    /// <typeparam name="T">The task type.</typeparam>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="value"/> is <c>null</c>.
    /// </exception>
    /// <returns>
    /// A value indicating whether the add was successful.
    /// </returns>
    bool TryAdd<T>(TaskCacheKey key, T value) where T : Task;

    /// <summary>
    /// Tries to add a single task to the cache. It does nothing if the
    /// task exists already.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <param name="createTask">A factory to create the new task.</param>
    /// <typeparam name="T">The task type.</typeparam>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="createTask"/> is <c>null</c>.
    /// </exception>
    /// <returns>
    /// A value indicating whether the add was successful.
    /// </returns>
    bool TryAdd<T>(TaskCacheKey key, Func<T> createTask) where T : Task;

    /// <summary>
    /// Removes a specific task from the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    bool TryRemove(TaskCacheKey key);

    /// <summary>
    /// Clears the complete cache.
    /// </summary>
    void Clear();
}