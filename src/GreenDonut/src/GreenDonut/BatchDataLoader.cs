namespace GreenDonut;

/// <summary>
/// The BatchDataLoader is the most commonly used variant of DataLoader and is optimized to
/// fetch multiple items in a single batch from the database.
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
public abstract class BatchDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDataLoader{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="batchScheduler">
    /// A scheduler to tell the <c>DataLoader</c> when to dispatch buffered batches.
    /// </param>
    /// <param name="options">
    /// An options object to configure the behavior of this particular
    /// <see cref="BatchDataLoader{TKey, TValue}"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="options"/> is <c>null</c>.
    /// </exception>
    protected BatchDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
    }

    /// <inheritdoc />
    protected internal sealed override async ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
    {
        var result =
            await LoadBatchAsync(keys, cancellationToken)
                .ConfigureAwait(false);

        CopyResults(keys, results.Span, result);
    }

    private static void CopyResults(
        IReadOnlyList<TKey> keys,
        Span<Result<TValue?>> results,
        IReadOnlyDictionary<TKey, TValue> resultMap)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            if (resultMap.TryGetValue(keys[i], out var value))
            {
                results[i] = value;
            }
            else
            {
                results[i] = null;
            }
        }
    }

    /// <summary>
    /// Loads the data for a batch from the data source.
    /// </summary>
    /// <param name="keys">The keys that shall be fetched in a batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a dictionary holding the fetched data.
    /// </returns>
    protected abstract Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchAsync(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken);
}

/// <summary>
/// The BatchDataLoader is the most commonly used variant of DataLoader and is optimized to
/// fetch multiple items in a single batch from the database.
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
public abstract class StatefulBatchDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDataLoader{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="batchScheduler">
    /// A scheduler to tell the <c>DataLoader</c> when to dispatch buffered batches.
    /// </param>
    /// <param name="options">
    /// An options object to configure the behavior of this particular
    /// <see cref="BatchDataLoader{TKey, TValue}"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="options"/> is <c>null</c>.
    /// </exception>
    protected StatefulBatchDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
    }

    /// <inheritdoc />
    protected internal sealed override async ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
    {
        var result =
            await LoadBatchAsync(keys, context, cancellationToken)
                .ConfigureAwait(false);

        CopyResults(keys, results.Span, result);
    }

    private static void CopyResults(
        IReadOnlyList<TKey> keys,
        Span<Result<TValue?>> results,
        IReadOnlyDictionary<TKey, TValue> resultMap)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            if (resultMap.TryGetValue(keys[i], out var value))
            {
                results[i] = value;
            }
            else
            {
                results[i] = null;
            }
        }
    }

    /// <summary>
    /// Loads the data for a batch from the data source.
    /// </summary>
    /// <param name="context">Represents the immutable fetch context.</param>
    /// <param name="keys">The keys that shall be fetched in a batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a dictionary holding the fetched data.
    /// </returns>
    protected abstract Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchAsync(
        IReadOnlyList<TKey> keys,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken);
}
