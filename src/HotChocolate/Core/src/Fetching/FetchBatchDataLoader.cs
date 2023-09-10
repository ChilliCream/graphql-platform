using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

#nullable enable

namespace HotChocolate.Fetching;

internal sealed class FetchBatchDataLoader<TKey, TValue>
    : BatchDataLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly FetchBatch<TKey, TValue> _fetch;

    public FetchBatchDataLoader(
        string key,
        FetchBatch<TKey, TValue> fetch,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        CacheKeyType = $"{GetCacheKeyType(GetType())}-{key}";
    }

    protected override string CacheKeyType { get; }

    protected override Task<IReadOnlyDictionary<TKey, TValue>> LoadBatchAsync(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken) =>
        _fetch(keys, cancellationToken);
}
