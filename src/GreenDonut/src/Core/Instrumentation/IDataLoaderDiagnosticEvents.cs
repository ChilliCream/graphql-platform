namespace GreenDonut;

/// <summary>
/// This interfaces specifies the DataLoader diagnostics events.
/// </summary>
public interface IDataLoaderDiagnosticEvents
{
    /// <summary>
    /// This event is raised whenever a DataLoader can resolve a request from the cache.
    /// </summary>
    /// <param name="dataLoader">The DataLoader that resolved the item.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="task">The task that has been resolved.</param>
    void ResolvedTaskFromCache(
        IDataLoader dataLoader,
        PromiseCacheKey cacheKey,
        Task task);

    /// <summary>
    /// This event is raised whenever a DataLoader batch is started to being executed.
    /// </summary>
    /// <param name="dataLoader">The DataLoader the batch belongs to.</param>
    /// <param name="keys">The keys that are being resolved.</param>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <returns>
    /// Returns the scope that represents the execution of the batch.
    /// </returns>
    IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
        where TKey : notnull;

    /// <summary>
    /// This event is raised whenever the executed batch yielded a result.
    /// </summary>
    /// <param name="keys">The keys that are being resolved.</param>
    /// <param name="values">The values that have been resolved.</param>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    void BatchResults<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        ReadOnlySpan<Result<TValue?>> values)
        where TKey : notnull;

    /// <summary>
    /// This event is raised whenever the executed batch had an error resolving the batch.
    /// </summary>
    /// <param name="keys">The keys that are being resolved.</param>
    /// <param name="error">The error that was thrown.</param>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    void BatchError<TKey>(
        IReadOnlyList<TKey> keys,
        Exception error)
        where TKey : notnull;

    /// <summary>
    /// This event is raised whenever there is an error for a specific key
    /// while resolving the batch.
    /// </summary>
    /// <param name="key">The key that is being resolved.</param>
    /// <param name="error">The error that was thrown.</param>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    void BatchItemError<TKey>(
        TKey key,
        Exception error)
        where TKey : notnull;
}
