namespace GreenDonut;

public abstract class CacheDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    where TKey : notnull
{
    protected CacheDataLoader(DataLoaderOptions options)
        : base(AutoBatchScheduler.Default, CreateLocalOptions(options))
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Cache is null)
        {
            throw new ArgumentException(
                "A cache must be provided when using the CacheDataLoader.",
                nameof(options));
        }
    }

    protected internal sealed override async ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            try
            {
                var value = await LoadSingleAsync(keys[i], cancellationToken).ConfigureAwait(false);
                results.Span[i] = value;

            }
            catch (Exception ex)
            {
                results.Span[i] = ex;
            }
        }
    }

    protected abstract Task<TValue> LoadSingleAsync(
        TKey key,
        CancellationToken cancellationToken);

    private static DataLoaderOptions CreateLocalOptions(DataLoaderOptions options)
    {
        var local = options.Copy();
        local.MaxBatchSize = 1;
        return local;
    }
}

public abstract class StatefulCacheDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    where TKey : notnull
{
    protected StatefulCacheDataLoader(DataLoaderOptions options)
        : base(AutoBatchScheduler.Default, CreateLocalOptions(options))
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Cache is null)
        {
            throw new ArgumentException(
                "A cache must be provided when using the CacheDataLoader.",
                nameof(options));
        }
    }

    protected internal sealed override async ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            try
            {
                var value = await LoadSingleAsync(keys[i], context, cancellationToken).ConfigureAwait(false);
                results.Span[i] = value;

            }
            catch (Exception ex)
            {
                results.Span[i] = ex;
            }
        }
    }

    protected abstract Task<TValue> LoadSingleAsync(
        TKey key,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken);

    private static DataLoaderOptions CreateLocalOptions(DataLoaderOptions options)
    {
        var local = options.Copy();
        local.MaxBatchSize = 1;
        return local;
    }
}
