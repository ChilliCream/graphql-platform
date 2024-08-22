namespace GreenDonut;

/// <summary>
/// The GroupedDataLoader is used to fetch a collection of items for
/// a single provided key in a batch.
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
public abstract class GroupedDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue[]>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupedDataLoader{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="batchScheduler">
    /// A scheduler to tell the <c>DataLoader</c> when to dispatch buffered batches.
    /// </param>
    /// <param name="options">
    /// An options object to configure the behavior of this particular
    /// <see cref="GroupedDataLoader{TKey, TValue}"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="options"/> is <c>null</c>.
    /// </exception>
    protected GroupedDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    { }

    /// <inheritdoc />
    protected internal sealed override async ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue[]?>> results,
        DataLoaderFetchContext<TValue[]> context,
        CancellationToken cancellationToken)
    {
        var result =
            await LoadGroupedBatchAsync(keys, cancellationToken)
                .ConfigureAwait(false);

        CopyResults(keys, results.Span, result);
    }

    private static void CopyResults(
        IReadOnlyList<TKey> keys,
        Span<Result<TValue[]?>> results,
        ILookup<TKey, TValue> resultLookup)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            results[i] = resultLookup[keys[i]].ToArray();
        }
    }

    /// <summary>
    /// Loads the data for a grouped batch from the data source.
    /// </summary>
    /// <param name="keys">The keys that shall be fetched in a batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a lookup holding the fetched data.
    /// </returns>
    protected abstract Task<ILookup<TKey, TValue>> LoadGroupedBatchAsync(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken);
}

/// <summary>
/// The GroupedDataLoader is used to fetch a collection of items for
/// a single provided key in a batch.
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
public abstract class StatefulGroupedDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue[]>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupedDataLoader{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="batchScheduler">
    /// A scheduler to tell the <c>DataLoader</c> when to dispatch buffered batches.
    /// </param>
    /// <param name="options">
    /// An options object to configure the behavior of this particular
    /// <see cref="GroupedDataLoader{TKey, TValue}"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="options"/> is <c>null</c>.
    /// </exception>
    protected StatefulGroupedDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    { }

    /// <inheritdoc />
    protected internal sealed override async ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue[]?>> results,
        DataLoaderFetchContext<TValue[]> context,
        CancellationToken cancellationToken)
    {
        var result =
            await LoadGroupedBatchAsync(keys, context, cancellationToken)
                .ConfigureAwait(false);

        CopyResults(keys, results.Span, result);
    }

    private static void CopyResults(
        IReadOnlyList<TKey> keys,
        Span<Result<TValue[]?>> results,
        ILookup<TKey, TValue> resultLookup)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            results[i] = resultLookup[keys[i]].ToArray();
        }
    }

    /// <summary>
    /// Loads the data for a grouped batch from the data source.
    /// </summary>
    /// <param name="keys">The keys that shall be fetched in a batch.</param>
    /// <param name="context">Represents the immutable fetch context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a lookup holding the fetched data.
    /// </returns>
    protected abstract Task<ILookup<TKey, TValue>> LoadGroupedBatchAsync(
        IReadOnlyList<TKey> keys,
        DataLoaderFetchContext<TValue[]> context,
        CancellationToken cancellationToken);
}
