using GreenDonut;

namespace HotChocolate.Fetching;

internal sealed class AdHocCacheDataLoader<TKey, TValue> : CacheDataLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly FetchCache<TKey, TValue> _fetch;

    public AdHocCacheDataLoader(string key, FetchCache<TKey, TValue> fetch, DataLoaderOptions options)
        : base(options)
    {
        _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        CacheKeyType = $"{GetCacheKeyType(GetType())}-{key}";
    }

    protected override string CacheKeyType { get; }

    protected override Task<TValue> LoadSingleAsync(
        TKey key,
        CancellationToken cancellationToken)
        => _fetch(key, cancellationToken);
}
