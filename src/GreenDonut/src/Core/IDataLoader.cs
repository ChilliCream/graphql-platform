using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut
{
    #region IDataLoader

    /// <summary>
    /// A <c>DataLoader</c> creates a public API for loading data from a
    /// particular data back-end with unique keys such as the `id` column of a
    /// SQL table or document name in a MongoDB database, given a batch loading
    /// function. -- facebook
    ///
    /// Each <c>DataLoader</c> instance contains a unique memoized cache. Use
    /// caution when used in long-lived applications or those which serve many
    /// users with different access permissions and consider creating a new
    /// instance per web request. -- facebook
    /// </summary>
    public interface IDataLoader
    {
        /// <summary>
        /// Raises when an incoming data request is added to the buffer. Will
        /// never be raised if batching is disabled.
        /// </summary>
        event RequestBufferedEventHandler RequestBuffered;

        /// <summary>
        /// Gets the current count of buffered data requests waiting for being
        /// dispatched as batches. Will always return <c>0</c> if batching is
        /// disabled.
        /// </summary>
        int BufferedRequests { get; }

        /// <summary>
        /// Gets the current count of cached values. Will always return
        /// <c>0</c> if caching is disabled.
        /// </summary>
        int CachedValues { get; }

        /// <summary>
        /// Empties the complete cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Dispatches one or more batch requests. In case of auto dispatching
        /// we just trigger an implicit dispatch which could mean to interrupt
        /// a wait delay. Whereas in a manual dispatch scenario it could mean
        /// to dispatch explicitly.
        /// </summary>
        Task DispatchAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Loads a single value by key. This call may return a cached value
        /// or enqueues this single request for bacthing if enabled.
        /// </summary>
        /// <param name="key">A unique key.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        Task<object> LoadAsync(
            object key,
            CancellationToken cancellationToken);

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <param name="keys">A list of unique keys.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A list of values in the same order as the provided keys.
        /// </returns>
        Task<IReadOnlyList<object>> LoadAsync(
            CancellationToken cancellationToken,
            params object[] keys);

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <param name="keys">A list of unique keys.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A list of values in the same order as the provided keys.
        /// </returns>
        Task<IReadOnlyList<object>> LoadAsync(
            IReadOnlyCollection<object> keys,
            CancellationToken cancellationToken);

        /// <summary>
        /// Removes a single entry from the cache.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        void Remove(object key);

        /// <summary>
        /// Adds a new entry to the cache if not already exists.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <param name="value">A cache entry value.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="value"/> is <c>null</c>.
        /// </exception>
        void Set(object key, Task<object> value);
    }

    #endregion

    #region IDataLoader<TKey, TValue>

    /// <summary>
    /// A <c>DataLoader</c> creates a public API for loading data from a
    /// particular data back-end with unique keys such as the `id` column of a
    /// SQL table or document name in a MongoDB database, given a batch loading
    /// function. -- facebook
    ///
    /// Each <c>DataLoader</c> instance contains a unique memoized cache. Use
    /// caution when used in long-lived applications or those which serve many
    /// users with different access permissions and consider creating a new
    /// instance per web request. -- facebook
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TValue">A value type.</typeparam>
    public interface IDataLoader<TKey, TValue>
        : IDataLoader
    {
        /// <summary>
        /// Loads a single value by key. This call may return a cached value
        /// or enqueues this single request for bacthing if enabled.
        /// </summary>
        /// <param name="key">A unique key.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        Task<TValue> LoadAsync(TKey key, CancellationToken cancellationToken);

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <param name="keys">A list of unique keys.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A list of values in the same order as the provided keys.
        /// </returns>
        Task<IReadOnlyList<TValue>> LoadAsync(
            CancellationToken cancellationToken,
            params TKey[] keys);

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <param name="keys">A list of unique keys.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A list of values in the same order as the provided keys.
        /// </returns>
        Task<IReadOnlyList<TValue>> LoadAsync(
            IReadOnlyCollection<TKey> keys,
            CancellationToken cancellationToken);

        /// <summary>
        /// Removes a single entry from the cache.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        void Remove(TKey key);

        /// <summary>
        /// Adds a new entry to the cache if not already exists.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <param name="value">A cache entry value.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="value"/> is <c>null</c>.
        /// </exception>
        void Set(TKey key, Task<TValue> value);
    }

    #endregion
}
