namespace GreenDonut.Internals;

/// <summary>
/// This helper allows to access internal members of the
/// <see cref="DataLoaderBase{TKey, TValue}"/>
/// for branching purposes.
/// </summary>
public static class DataLoaderHelper
{
    /// <summary>
    /// Gets the options of the <see cref="DataLoaderBase{TKey, TValue}"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The <see cref="DataLoaderBase{TKey, TValue}"/> instance.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns the options of the <see cref="DataLoaderBase{TKey, TValue}"/>.
    /// </returns>
    public static DataLoaderOptions GetOptions<TKey, TValue>(
        DataLoaderBase<TKey, TValue> dataLoader)
        where TKey : notnull
        => dataLoader.Options;

    /// <summary>
    /// Gets the batch scheduler of the <see cref="DataLoaderBase{TKey, TValue}"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The <see cref="DataLoaderBase{TKey, TValue}"/> instance.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns the batch scheduler of the <see cref="DataLoaderBase{TKey, TValue}"/>.
    /// </returns>
    public static IBatchScheduler GetBatchScheduler<TKey, TValue>(
        DataLoaderBase<TKey, TValue> dataLoader)
        where TKey : notnull
        => dataLoader.BatchScheduler;

    /// <summary>
    /// Gets the cache key type of the <see cref="DataLoaderBase{TKey, TValue}"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The <see cref="DataLoaderBase{TKey, TValue}"/> instance.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns the cache key type of the <see cref="DataLoaderBase{TKey, TValue}"/>.
    /// </returns>
    public static string GetCacheKeyType<TKey, TValue>(
        DataLoaderBase<TKey, TValue> dataLoader)
        where TKey : notnull
        => dataLoader.CacheKeyType;

    /// <summary>
    /// Fetches the data for the provided keys by using the fetch method of
    /// the provided <see cref="DataLoaderBase{TKey, TValue}"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The <see cref="DataLoaderBase{TKey, TValue}"/> instance.
    /// </param>
    /// <param name="keys">
    /// The keys for which the data shall be fetched.
    /// </param>
    /// <param name="results">
    /// The results that shall be filled with the fetched data.
    /// </param>
    /// <param name="context">
    /// The fetch context that shall be used for the fetch operation.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token that shall be used for the fetch operation.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns the task representing the fetch operation.
    /// </returns>
    public static ValueTask FetchAsync<TKey, TValue>(
        DataLoaderBase<TKey, TValue> dataLoader,
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken) where TKey : notnull
        => dataLoader.FetchAsync(keys, results, context, cancellationToken);
}
