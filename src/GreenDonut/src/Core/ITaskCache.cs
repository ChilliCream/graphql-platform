using System;
using System.Threading.Tasks;

namespace GreenDonut
{
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

        T GetOrAddTask<T>(TaskCacheKey key, Func<T> createTask) where T : Task;

        /// <summary>
        /// Tries to add a single entry to the cache. It does nothing if the
        /// cache entry exists already.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <param name="value">A cache entry value.</param>
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
        /// Removes a specific entry from the cache.
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
}
