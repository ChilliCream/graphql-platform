using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace GreenDonut;

public abstract class CacheDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    where TKey : notnull
{
    protected CacheDataLoader(DataLoaderOptions? options = null)
        : base(
            AutoBatchScheduler.Default,
            options is null
                ? new DataLoaderOptions { MaxBatchSize = 1, }
                : CreateLocalOptions(options))
    { }

    protected sealed override async ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue>> results,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            try
            {
                var value = await LoadSingleAsync(keys[i], cancellationToken)
                    .ConfigureAwait(false);
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