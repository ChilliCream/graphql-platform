using GreenDonut;

namespace HotChocolate.Fetching;

internal sealed class AdHocGroupedDataLoader<TKey, TValue> : GroupedDataLoader<TKey, TValue> where TKey : notnull
{
    private readonly FetchGroup<TKey, TValue> _fetch;

    public AdHocGroupedDataLoader(
        string key,
        FetchGroup<TKey, TValue> fetch,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        CacheKeyType = $"{GetCacheKeyType(GetType())}-{key}";
    }

    protected override string CacheKeyType { get; }

    protected override Task<ILookup<TKey, TValue>> LoadGroupedBatchAsync(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
        => _fetch(keys, cancellationToken);
}
