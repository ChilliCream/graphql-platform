using System;
using System.Threading.Tasks;

namespace GreenDonut
{
    /// <summary>
    /// A memorization cache for <c>DataLoader</c>.
    /// </summary>
    /// <typeparam name="TValue">A value type.</typeparam>
    public interface ITaskCache<TValue>
    {
        /// <summary>
        /// Gets the maximum size of the cache.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the sliding expiration for the cache entries.
        /// </summary>
        TimeSpan SlidingExpirartion { get; }

        /// <summary>
        /// Gets the count of the entries inside the cache.
        /// </summary>
        int Usage { get; }

        /// <summary>
        /// Clears the complete cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes a specific entry from the cache.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        void Remove(object key);

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
        bool TryAdd(object key, Task<TValue> value);

        /// <summary>
        /// Tries to gets a single entry from the cache.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <param name="value">A single cache entry value.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A value indicating whether the get request returned an entry.
        /// </returns>
        bool TryGetValue(object key, out Task<TValue> value);
    }
}
