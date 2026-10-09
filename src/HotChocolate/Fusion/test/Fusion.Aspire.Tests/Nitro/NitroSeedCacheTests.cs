using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroSeedCacheTests : IDisposable
{
    private static readonly Uri s_apiUrl = new("https://api.chillicream.com/");
    private static readonly DateTimeOffset s_now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly NitroTestDirectory _directory = new();
    private readonly FakeTimeProvider _timeProvider = new(s_now);

    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task TryPromoteAsync_Should_WriteTheArchiveAndTheMetadata_When_ItIsAnArchive()
    {
        // arrange
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        var tempFilePath = await WriteTempAsync(cache, key, "products", "reviews");

        // act
        var entry = await cache.TryPromoteAsync(
            key,
            tempFilePath,
            new RecordingLogger<NitroSeedCacheTests>(),
            TestContext.Current.CancellationToken);

        // assert
        // the content marker is a hash of the archive, which is not stable across runs
        var metadata = MaskContentMarker(
            await File.ReadAllTextAsync(
                cache.GetMetadataPath(key),
                TestContext.Current.CancellationToken));

        Assert.Equal(s_now, entry!.DownloadedAt);
        Assert.Equal(
            ["products", "reviews"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                entry.FilePath,
                TestContext.Current.CancellationToken));
        metadata.MatchInlineSnapshot(
            """
            {
              "apiUrl": "https://api.chillicream.com/",
              "apiId": "api-1",
              "stage": "dev",
              "downloadedAt": "2026-07-29T12:00:00+00:00",
              "fusionVersion": "2.0.0",
              "contentMarker": "<marker>"
            }
            """);
    }

    [Fact]
    public async Task TryGetAsync_Should_DiscardTheEntry_When_ThePromotionWasTorn()
    {
        // arrange
        // the archive of a later promotion is in place while the metadata of the earlier one
        // is still there, which is what an interrupted promotion leaves behind.
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");
        await File.WriteAllBytesAsync(
            cache.GetArchivePath(key),
            await NitroTestArchive.CreateAsync(
                TestContext.Current.CancellationToken,
                "products",
                "reviews"),
            TestContext.Current.CancellationToken);
        var logger = new RecordingLogger<NitroSeedCacheTests>();

        // act
        var entry = await cache.TryGetAsync(key, logger, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(entry);
        Assert.False(File.Exists(cache.GetArchivePath(key)));
        Assert.Equal(
            "The cached fusion configuration for the api api-1 and the stage dev at "
            + $"{cache.GetArchivePath(key)} was discarded because its metadata does not describe "
            + "the cached file, which means a previous download was interrupted while it was "
            + "promoted into the cache.",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task TryGetAsync_Should_ReturnNull_When_NoEntryExists()
    {
        // arrange
        var cache = CreateCache();

        // act
        var entry = await cache.TryGetAsync(
            new NitroSeedKey(s_apiUrl, "api-1", "dev"),
            new RecordingLogger<NitroSeedCacheTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(entry);
    }

    [Fact]
    public async Task TryGetAsync_Should_ReturnTheEntry_When_ItWasPromoted()
    {
        // arrange
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");

        // act
        var entry = await cache.TryGetAsync(
            key,
            new RecordingLogger<NitroSeedCacheTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(s_now, entry!.DownloadedAt);
        Assert.Equal(cache.GetArchivePath(key), entry.FilePath);
    }

    [Fact]
    public async Task TryPromoteAsync_Should_KeepTheCachedEntry_When_TheDownloadIsTruncated()
    {
        // arrange
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            "products",
            "reviews");
        var truncated = cache.CreateTempFilePath(key);
        await File.WriteAllBytesAsync(
            truncated,
            archive.AsSpan(0, archive.Length / 2).ToArray(),
            TestContext.Current.CancellationToken);

        // act
        var promoted = await cache.TryPromoteAsync(
            key,
            truncated,
            new RecordingLogger<NitroSeedCacheTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(promoted);
        Assert.Equal(
            ["products"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                cache.GetArchivePath(key),
                TestContext.Current.CancellationToken));
        Assert.False(File.Exists(truncated));
    }

    [Fact]
    public async Task TryPromoteAsync_Should_KeepTheCachedEntry_When_TheDownloadIsNotAnArchive()
    {
        // arrange
        // a captive portal answers with HTML and the status code 200
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");
        var html = cache.CreateTempFilePath(key);
        await File.WriteAllTextAsync(
            html,
            "<html><body>Sign in to the network</body></html>",
            TestContext.Current.CancellationToken);
        var logger = new RecordingLogger<NitroSeedCacheTests>();

        // act
        var promoted = await cache.TryPromoteAsync(
            key,
            html,
            logger,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(promoted);
        Assert.Equal(
            ["products"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                cache.GetArchivePath(key),
                TestContext.Current.CancellationToken));
        Assert.Equal(
            "The fusion configuration that was downloaded for the api api-1 and the stage dev "
            + "from https://api.chillicream.com/ is not a valid fusion archive and was discarded.",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task TryGetAsync_Should_DiscardTheEntry_When_TheArchiveIsCorrupt()
    {
        // arrange
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");
        await File.WriteAllBytesAsync(
            cache.GetArchivePath(key),
            Encoding.UTF8.GetBytes("not an archive"),
            TestContext.Current.CancellationToken);
        var logger = new RecordingLogger<NitroSeedCacheTests>();

        // act
        var entry = await cache.TryGetAsync(key, logger, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(entry);
        Assert.False(File.Exists(cache.GetArchivePath(key)));
        Assert.Equal(
            "The cached fusion configuration for the api api-1 and the stage dev at "
            + $"{cache.GetArchivePath(key)} was discarded because it is not a valid fusion "
            + "archive.",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task TryGetAsync_Should_DiscardTheEntry_When_TheMetadataIsMissing()
    {
        // arrange
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");
        File.Delete(cache.GetMetadataPath(key));

        // act
        var entry = await cache.TryGetAsync(
            key,
            new RecordingLogger<NitroSeedCacheTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(entry);
        Assert.False(File.Exists(cache.GetArchivePath(key)));
    }

    [Fact]
    public async Task TryGetAsync_Should_DiscardTheEntry_When_TheMetadataDescribesAnotherTuple()
    {
        // arrange
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");
        await File.WriteAllTextAsync(
            cache.GetMetadataPath(key),
            """
            {
              "apiUrl": "https://api.chillicream.com/",
              "apiId": "api-2",
              "stage": "dev",
              "downloadedAt": "2026-07-29T12:00:00+00:00",
              "fusionVersion": "2.0.0"
            }
            """,
            TestContext.Current.CancellationToken);
        var logger = new RecordingLogger<NitroSeedCacheTests>();

        // act
        var entry = await cache.TryGetAsync(key, logger, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(entry);
        Assert.EndsWith(
            "was discarded because its metadata describes another api url, api or stage.",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task TryGetAsync_Should_DiscardTheEntry_When_ItTargetsAnotherFusionVersion()
    {
        // arrange
        var cache = CreateCache();
        var key = new NitroSeedKey(s_apiUrl, "api-1", "dev");
        await PromoteAsync(cache, key, "products");
        await File.WriteAllTextAsync(
            cache.GetMetadataPath(key),
            """
            {
              "apiUrl": "https://api.chillicream.com/",
              "apiId": "api-1",
              "stage": "dev",
              "downloadedAt": "2026-07-29T12:00:00+00:00",
              "fusionVersion": "1.0.0"
            }
            """,
            TestContext.Current.CancellationToken);
        var logger = new RecordingLogger<NitroSeedCacheTests>();

        // act
        var entry = await cache.TryGetAsync(key, logger, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(entry);
        Assert.EndsWith(
            "was discarded because it was downloaded for the gateway format version 1.0.0 "
            + "instead of 2.0.0.",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task TryGetAsync_Should_KeepEntriesApart_When_TheApiUrlApiIdOrStageDiffer()
    {
        // arrange
        var cache = CreateCache();
        var keys = new[]
        {
            new NitroSeedKey(s_apiUrl, "api-1", "dev"),
            new NitroSeedKey(s_apiUrl, "api-1", "prod"),
            new NitroSeedKey(s_apiUrl, "api-2", "dev"),
            new NitroSeedKey(new Uri("https://nitro.example.com/"), "api-1", "dev")
        };

        // act
        foreach (var key in keys)
        {
            await PromoteAsync(cache, key, $"{key.ApiId}-{key.Stage}-{key.ApiUrl.Host}");
        }

        // assert
        var names = new List<string>();

        foreach (var key in keys)
        {
            var entry = await cache.TryGetAsync(
                key,
                new RecordingLogger<NitroSeedCacheTests>(),
                TestContext.Current.CancellationToken);

            names.AddRange(
                await NitroTestArchive.ReadSourceSchemaNamesAsync(
                    entry!.FilePath,
                    TestContext.Current.CancellationToken));
        }

        Assert.Equal(
            [
                "api-1-dev-api.chillicream.com",
                "api-1-prod-api.chillicream.com",
                "api-2-dev-api.chillicream.com",
                "api-1-dev-nitro.example.com"
            ],
            names);
    }

    private static string MaskContentMarker(string metadata)
    {
        using var document = JsonDocument.Parse(metadata);
        var contentMarker = document.RootElement.GetProperty("contentMarker").GetString()!;

        return metadata.Replace(contentMarker, "<marker>", StringComparison.Ordinal);
    }

    private NitroSeedCache CreateCache() => new(_directory.GetPath("cache"), _timeProvider);

    private async Task PromoteAsync(
        NitroSeedCache cache,
        NitroSeedKey key,
        params string[] sourceSchemaNames)
    {
        var tempFilePath = await WriteTempAsync(cache, key, sourceSchemaNames);

        await cache.TryPromoteAsync(
            key,
            tempFilePath,
            new RecordingLogger<NitroSeedCacheTests>(),
            TestContext.Current.CancellationToken);
    }

    private static async Task<string> WriteTempAsync(
        NitroSeedCache cache,
        NitroSeedKey key,
        params string[] sourceSchemaNames)
    {
        var tempFilePath = cache.CreateTempFilePath(key);
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            sourceSchemaNames);

        await File.WriteAllBytesAsync(
            tempFilePath,
            archive,
            TestContext.Current.CancellationToken);

        return tempFilePath;
    }
}
