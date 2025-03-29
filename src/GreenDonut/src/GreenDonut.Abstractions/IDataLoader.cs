using System.Collections.Immutable;

namespace GreenDonut;

/// <summary>
/// <para>
/// A <c>DataLoader</c> creates a public API for loading data from a
/// particular data back-end with unique keys such as the `id` column of a
/// SQL table or document name in a MongoDB database, given a batch loading
/// function. -- facebook
/// </para>
/// <para>
/// Each <c>DataLoader</c> instance contains a unique memoized cache. Use
/// caution when used in long-lived applications or those which serve many
/// users with different access permissions and consider creating a new
/// instance per web request. -- facebook
/// </para>
/// </summary>
public interface IDataLoader
{
    /// <summary>
    /// Gets or sets the context data which can be used to store
    /// transient state on the DataLoader.
    /// </summary>
    IImmutableDictionary<string, object?> ContextData { get; set; }

    /// <summary>
    /// Loads a single value by key. This call may return a cached value
    /// or enqueues this single request for batching if enabled.
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
    Task<object?> LoadAsync(
        object key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads multiple values by keys. This call may return cached values
    /// and enqueues requests which were not cached for batching if
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
    Task<IReadOnlyList<object?>> LoadAsync(
        IReadOnlyCollection<object> keys,
        CancellationToken cancellationToken = default);

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
    void SetCacheEntry(object key, Task<object?> value);

    /// <summary>
    /// Removes a single entry from the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    void RemoveCacheEntry(object key);

    /// <summary>
    /// Empties the complete cache.
    /// </summary>
    void ClearCache();

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
    [Obsolete("Use SetCacheEntry instead.")]
    void Set(object key, Task<object?> value);

    /// <summary>
    /// Removes a single entry from the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    [Obsolete("Use RemoveCacheEntry instead.")]
    void Remove(object key);

    /// <summary>
    /// Empties the complete cache.
    /// </summary>
    [Obsolete("Use ClearCache instead.")]
    void Clear();
}

/// <summary>
/// <para>
/// A <c>DataLoader</c> creates a public API for loading data from a
/// particular data back-end with unique keys such as the `id` column of a
/// SQL table or document name in a MongoDB database, given a batch loading
/// function. -- facebook
/// </para>
/// <para>
/// Each <c>DataLoader</c> instance contains a unique memoized cache. Use
/// caution when used in long-lived applications or those which serve many
/// users with different access permissions and consider creating a new
/// instance per web request. -- facebook
/// </para>
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
public interface IDataLoader<in TKey, TValue>
    : IDataLoader
    where TKey : notnull
{
    /// <summary>
    /// Loads a single value by key. This call may return a cached value
    /// or enqueues this single request for batching if enabled.
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
    Task<TValue?> LoadAsync(
        TKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads multiple values by keys. This call may return cached values
    /// and enqueues requests which were not cached for batching if
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
    Task<IReadOnlyList<TValue?>> LoadAsync(
        IReadOnlyCollection<TKey> keys,
        CancellationToken cancellationToken = default);

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
    void SetCacheEntry(TKey key, Task<TValue?> value);

    /// <summary>
    /// Removes a single entry from the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    void RemoveCacheEntry(TKey key);

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
    [Obsolete("Use SetCacheEntry instead.")]
    void Set(TKey key, Task<TValue?> value);

    /// <summary>
    /// Removes a single entry from the cache.
    /// </summary>
    /// <param name="key">A cache entry key.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    [Obsolete("Use RemoveCacheEntry instead.")]
    void Remove(TKey key);

    /// <summary>
    /// Branches the current <c>DataLoader</c>.
    /// </summary>
    /// <param name="key">
    /// A unique key to identify the branch.
    /// </param>
    /// <param name="createBranch">
    /// Creates the branch of the current <c>DataLoader</c>.
    /// </param>
    /// <param name="state">
    /// A custom state object that is passed to the branch factory.
    /// </param>
    /// <returns>
    /// A new <c>DataLoader</c> instance.
    /// </returns>
    IDataLoader Branch<TState>(
        string key,
        CreateDataLoaderBranch<TKey, TValue, TState> createBranch,
        TState state);
}
