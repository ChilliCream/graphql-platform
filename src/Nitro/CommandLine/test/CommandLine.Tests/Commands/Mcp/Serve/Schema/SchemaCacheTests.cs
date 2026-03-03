using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Schema;

public sealed class SchemaCacheTests
{
    private const string TestSdl = """
        type Query {
          hello: String
        }
        """;

    [Fact]
    public async Task GetOrBuildAsync_Stores_And_Retrieves()
    {
        var cache = new SchemaCache();
        var fetchCount = 0;

        Task<(string? Sdl, string? ETag)> Fetch(string? etag)
        {
            fetchCount++;
            return Task.FromResult<(string?, string?)>((TestSdl, "etag-1"));
        }

        var index1 = await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);
        var index2 = await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);

        Assert.Same(index1, index2);
        Assert.Equal(1, fetchCount);
    }

    [Fact]
    public async Task GetOrBuildAsync_Different_Keys_Fetch_Separately()
    {
        var cache = new SchemaCache();
        var fetchCount = 0;

        Task<(string? Sdl, string? ETag)> Fetch(string? etag)
        {
            fetchCount++;
            return Task.FromResult<(string?, string?)>((TestSdl, null));
        }

        var index1 = await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);
        var index2 = await cache.GetOrBuildAsync(
            "api2", "prod", Fetch, CancellationToken.None);

        Assert.NotSame(index1, index2);
        Assert.Equal(2, fetchCount);
    }

    [Fact]
    public async Task GetOrBuildAsync_Stale_Entry_Refetches()
    {
        var cache = new SchemaCache(staleness: TimeSpan.Zero);
        var fetchCount = 0;

        Task<(string? Sdl, string? ETag)> Fetch(string? etag)
        {
            fetchCount++;
            return Task.FromResult<(string?, string?)>((TestSdl, null));
        }

        await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);

        // With zero staleness, the next call should refetch
        await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);

        Assert.Equal(2, fetchCount);
    }

    [Fact]
    public async Task GetOrBuildAsync_NullSdl_Reuses_Cached_Index()
    {
        var cache = new SchemaCache();
        var callNumber = 0;

        Task<(string? Sdl, string? ETag)> Fetch(string? etag)
        {
            callNumber++;
            if (callNumber == 1)
            {
                return Task.FromResult<(string?, string?)>((TestSdl, "etag-1"));
            }

            // Second call returns null SDL (304-equivalent)
            return Task.FromResult<(string?, string?)>((null, "etag-1"));
        }

        // Use zero staleness so second call will attempt to refresh
        var cacheWithStaleness = new SchemaCache(staleness: TimeSpan.Zero);

        var index1 = await cacheWithStaleness.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);
        var index2 = await cacheWithStaleness.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);

        // Same index reused since the second fetch returned null SDL
        Assert.Same(index1, index2);
        Assert.Equal(2, callNumber);
    }

    [Fact]
    public async Task GetOrBuildAsync_Evicts_Oldest_When_Full()
    {
        var cache = new SchemaCache(maxEntries: 2);
        var fetchCount = 0;

        Task<(string? Sdl, string? ETag)> Fetch(string? etag)
        {
            fetchCount++;
            return Task.FromResult<(string?, string?)>((TestSdl, null));
        }

        await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);
        await cache.GetOrBuildAsync(
            "api2", "prod", Fetch, CancellationToken.None);
        await cache.GetOrBuildAsync(
            "api3", "prod", Fetch, CancellationToken.None);

        Assert.Equal(3, fetchCount);

        // api1 was the oldest, so it should be evicted. Fetching it again triggers a refetch.
        fetchCount = 0;
        await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);
        Assert.Equal(1, fetchCount);
    }

    [Fact]
    public async Task GetOrBuildAsync_CaseInsensitive_Key()
    {
        var cache = new SchemaCache();
        var fetchCount = 0;

        Task<(string? Sdl, string? ETag)> Fetch(string? etag)
        {
            fetchCount++;
            return Task.FromResult<(string?, string?)>((TestSdl, null));
        }

        var index1 = await cache.GetOrBuildAsync(
            "API1", "Prod", Fetch, CancellationToken.None);
        var index2 = await cache.GetOrBuildAsync(
            "api1", "prod", Fetch, CancellationToken.None);

        Assert.Same(index1, index2);
        Assert.Equal(1, fetchCount);
    }
}
