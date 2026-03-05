using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

internal sealed class SchemaCache : IDisposable
{
    private readonly int _maxEntries;
    private readonly TimeSpan _staleness;
    private readonly Dictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SchemaCache(int maxEntries = 10, TimeSpan? staleness = null)
    {
        _maxEntries = maxEntries;
        _staleness = staleness ?? TimeSpan.FromMinutes(5);
    }

    public async Task<SchemaIndex> GetOrBuildAsync(
        string apiId,
        string stage,
        Func<string?, Task<(string? Sdl, string? ETag)>> fetchAsync,
        CancellationToken cancellationToken)
    {
        var key = apiId + ":" + stage;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_entries.TryGetValue(key, out var cached))
            {
                if (DateTimeOffset.UtcNow - cached.CachedAt < _staleness)
                {
                    return cached.Index;
                }
            }

            var existingETag = cached?.ETag;
            var (sdl, newETag) = await fetchAsync(existingETag);

            // If fetch returned null/empty SDL (304-equivalent), reuse cached
            if (string.IsNullOrEmpty(sdl))
            {
                if (cached is not null)
                {
                    var refreshed = cached with { CachedAt = DateTimeOffset.UtcNow };
                    _entries[key] = refreshed;
                    return refreshed.Index;
                }

                throw new InvalidOperationException(
                    $"Schema fetch for '{key}' returned no SDL and no cached entry exists.");
            }

            var index = SchemaIndexBuilder.Build(sdl);
            var entry = new CacheEntry(index, newETag, DateTimeOffset.UtcNow);

            EvictIfNeeded(key);
            _entries[key] = entry;

            return index;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EvictIfNeeded(string currentKey)
    {
        if (_entries.Count < _maxEntries)
        {
            return;
        }

        var oldestKey = (string?)null;
        var oldestTime = DateTimeOffset.MaxValue;

        foreach (var kvp in _entries)
        {
            if (kvp.Key != currentKey && kvp.Value.CachedAt < oldestTime)
            {
                oldestKey = kvp.Key;
                oldestTime = kvp.Value.CachedAt;
            }
        }

        if (oldestKey is not null)
        {
            _entries.Remove(oldestKey);
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private sealed record CacheEntry(SchemaIndex Index, string? ETag, DateTimeOffset CachedAt);
}
