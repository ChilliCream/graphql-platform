namespace GreenDonut;

/// <summary>
/// The <see cref="FetchOnceDataLoader{TValue}"/> fetches a single object and caches it.
/// </summary>
/// <typeparam name="TValue">A value type.</typeparam>
public abstract class FetchOnceDataLoader<TValue>
    : CacheDataLoader<string, TValue>
{
    protected FetchOnceDataLoader(DataLoaderOptions options)
        : base(options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Cache is null)
        {
            throw new ArgumentException(
                "A cache must be provided when using the FetchOnceDataLoader.",
                nameof(options));
        }
    }

    /// <summary>
    /// Loads a single value. This call may return a cached value
    /// or enqueues this single request for batching if enabled.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A single result which may contain a value or information about the
    /// error which may occurred during the call.
    /// </returns>
    public Task<TValue?> LoadAsync(CancellationToken cancellationToken)
        => LoadAsync("default", cancellationToken);

    protected sealed override Task<TValue> LoadSingleAsync(
        string key,
        CancellationToken cancellationToken)
        => LoadSingleAsync(cancellationToken);

    protected abstract Task<TValue> LoadSingleAsync(CancellationToken cancellationToken);
}
