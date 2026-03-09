using GreenDonut;

namespace HotChocolate.Fetching;

internal sealed class AdHocBatchDataLoader<TKey, TValue> : BatchDataLoader<TKey, TValue> where TKey : notnull
{
    private readonly FetchBatch<TKey, TValue> _fetch;

    public AdHocBatchDataLoader(
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
        CancellationToken cancellationToken)
        => _fetch(keys, cancellationToken);
}
