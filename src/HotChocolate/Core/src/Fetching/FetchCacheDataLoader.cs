using System;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

#nullable enable

namespace HotChocolate.Fetching;

internal sealed class FetchCacheDataLoader<TKey, TValue>
    : CacheDataLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly FetchCache<TKey, TValue> _fetch;

    public FetchCacheDataLoader(
        string key,
        FetchCache<TKey, TValue> fetch,
        DataLoaderOptions options)
        : base(options)
    {
        _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        CacheKeyType = $"{GetCacheKeyType(GetType())}-{key}";
    }

    protected override string CacheKeyType { get; }

    protected override Task<TValue> LoadSingleAsync(
        TKey key,
        CancellationToken cancellationToken) =>
        _fetch(key, cancellationToken);
}
