using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroSeedProviderTests : IAsyncLifetime
{
    private const string ApiId = "QXBpCmc1YzhkY2Uz";
    private const string Stage = "dev";
    private const string ApiUrlPlaceholder = "https://nitro.test/";
    private static readonly DateTimeOffset s_now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly NitroTestDirectory _directory = new();
    private readonly FakeTimeProvider _timeProvider = new(s_now);
    private readonly HttpClient _httpClient = new();
    private FakeNitroServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FakeNitroServer.StartAsync();

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        _directory.Dispose();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task GetSeedAsync_Should_ReportDownloaded_When_NitroServesTheConfiguration()
    {
        // arrange
        await ServeArchiveAsync("products", "reviews");
        var provider = CreateProvider();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        // the api lookup only runs on the failure path, so the download is the only request
        var request = Assert.Single(_server.Requests);

        Assert.Equal(NitroSeedOutcome.Downloaded, result.Outcome);
        Assert.Equal(s_now, result.DownloadedAt);
        Assert.StartsWith("/api/v1/apis/", request.Path, StringComparison.Ordinal);
        Assert.Equal(
            ["products", "reviews"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                result.FilePath!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSeedAsync_Should_FallBackWithAWarning_When_TheRetryBudgetIsExhausted()
    {
        // arrange
        var provider = await SeedTheCacheAsync();
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status503ServiceUnavailable);
        var logger = new RecordingLogger<NitroSeedProviderTests>();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            logger,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSeedOutcome.ServedFromCache, result.Outcome);
        Assert.Equal(s_now, result.DownloadedAt);
        Normalize(GetFallbackWarning(logger)).MatchInlineSnapshot(
            "A fresh fusion configuration could NOT be fetched from Nitro. The fusion "
            + "configuration for the api 'QXBpCmc1YzhkY2Uz' and the stage 'dev' could not be "
            + "downloaded from 'https://nitro.test/' after 2 attempts (Nitro returned the status "
            + "code 503.). Falling back to the fusion configuration that was downloaded at "
            + "2026-07-29 12:00:00Z, which may be out of date.");
    }

    [Fact]
    public async Task GetSeedAsync_Should_ReportUnavailable_When_TheDownloadFailsAndNoCacheExists()
    {
        // arrange
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status503ServiceUnavailable);
        var provider = CreateProvider(attemptsWithoutCachedSeed: 2);

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSeedOutcome.Unavailable, result.Outcome);
        Assert.Null(result.FilePath);
        Normalize(result.Message!).MatchInlineSnapshot(
            "The fusion configuration for the api 'QXBpCmc1YzhkY2Uz' and the stage 'dev' could "
            + "not be downloaded from 'https://nitro.test/' after 2 attempts (Nitro returned the "
            + "status code 503.).");
    }

    [Fact]
    public async Task GetSeedAsync_Should_KeepTheCachedEntry_When_TheDownloadIsTruncated()
    {
        // arrange
        var provider = await SeedTheCacheAsync();
        var newer = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            "products",
            "reviews");
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Archive(newer.AsSpan(0, newer.Length / 2).ToArray());

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSeedOutcome.ServedFromCache, result.Outcome);
        Assert.Equal(
            ["products"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                result.FilePath!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSeedAsync_Should_KeepTheCachedEntry_When_TheResponseIsNotAnArchive()
    {
        // arrange
        // a captive portal answers with HTML and the status code 200
        var provider = await SeedTheCacheAsync();
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Html("<html><body>Sign in to the network</body></html>");

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSeedOutcome.ServedFromCache, result.Outcome);
        Assert.Equal(
            ["products"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                result.FilePath!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSeedAsync_Should_FallBackWithoutContactingNitro_When_TheSessionExpired()
    {
        // arrange
        var provider = await SeedTheCacheAsync();
        var requestsAfterSeeding = _server.Requests.Count;
        var logger = new RecordingLogger<NitroSeedProviderTests>();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(CreateExpiredCredential()),
            ApiId,
            Stage,
            logger,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSeedOutcome.ServedFromCache, result.Outcome);
        Assert.Equal(requestsAfterSeeding, _server.Requests.Count);
        GetFallbackWarning(logger).MatchInlineSnapshot(
            "A fresh fusion configuration could NOT be fetched from Nitro. The Nitro session "
            + "stored at '/tmp/session.json' expired at 2026-07-29 11:00:00Z. Run 'nitro login' "
            + "to sign in again. Falling back to the fusion configuration that was downloaded at "
            + "2026-07-29 12:00:00Z, which may be out of date.");
    }

    [Fact]
    public async Task GetSeedAsync_Should_ReportUnavailable_When_TheSessionExpiredWithoutACache()
    {
        // arrange
        var provider = CreateProvider();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(CreateExpiredCredential()),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroSeedOutcome.Unavailable, result.Outcome);
        Assert.Empty(_server.Requests);
        Assert.Equal(
            "The Nitro session stored at '/tmp/session.json' expired at 2026-07-29 11:00:00Z. "
            + "Run 'nitro login' to sign in again.",
            result.Message);
    }

    [Fact]
    public async Task GetSeedAsync_Should_NameTheEnvironmentVariable_When_TheApiKeyIsRejected()
    {
        // arrange
        _server.DownloadHandler = _ => FakeNitroResponse.Status(StatusCodes.Status401Unauthorized);
        var provider = CreateProvider();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Normalize(result.Message!).MatchInlineSnapshot(
            "Nitro at 'https://nitro.test/' rejected the API key from the NITRO_API_KEY "
            + "environment variable (Nitro rejected the request with the status code 401.).");
    }

    [Fact]
    public async Task GetSeedAsync_Should_AskForALogin_When_TheSessionTokenIsRejected()
    {
        // arrange
        _server.DownloadHandler = _ => FakeNitroResponse.Status(StatusCodes.Status403Forbidden);
        var provider = CreateProvider();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromAccessToken("access-token")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Normalize(result.Message!).MatchInlineSnapshot(
            "Nitro at 'https://nitro.test/' rejected the session access token (Nitro rejected the "
            + "request with the status code 403.). Run 'nitro login' to sign in again.");
    }

    [Fact]
    public async Task GetSeedAsync_Should_NameTheApi_When_TheApiHasNoConfigurationForTheStage()
    {
        // arrange
        _server.DownloadHandler = _ => FakeNitroResponse.Status(StatusCodes.Status404NotFound);
        _server.GraphQLHandler = _ =>
            FakeNitroResponse.Json("""{"data":{"node":{"name":"Demo Api"}}}""");
        var provider = CreateProvider();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Normalize(result.Message!).MatchInlineSnapshot(
            "The api 'Demo Api' with the id 'QXBpCmc1YzhkY2Uz' has no fusion configuration for "
            + "the stage 'dev' in Nitro.");
    }

    [Fact]
    public async Task GetSeedAsync_Should_PointAtWithNitroApiId_When_TheApiIdIsUnknown()
    {
        // arrange
        _server.DownloadHandler = _ => FakeNitroResponse.Status(StatusCodes.Status404NotFound);
        _server.GraphQLHandler = _ => FakeNitroResponse.Json("""{"data":{"node":null}}""");
        var provider = CreateProvider();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Normalize(result.Message!).MatchInlineSnapshot(
            "Nitro at 'https://nitro.test/' knows no api with the id 'QXBpCmc1YzhkY2Uz'. Check "
            + "the api id that is passed to WithNitroApiId.");
    }

    [Fact]
    public async Task GetSeedAsync_Should_StayGeneric_When_TheApiLookupItselfFails()
    {
        // arrange
        _server.DownloadHandler = _ => FakeNitroResponse.Status(StatusCodes.Status404NotFound);
        _server.GraphQLHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status500InternalServerError);
        var provider = CreateProvider();
        var logger = new RecordingLogger<NitroSeedProviderTests>();

        // act
        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            logger,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Contains(
            logger.Entries,
            entry => entry.Message
                == "The Nitro api lookup for the api id QXBpCmc1YzhkY2Uz failed with the status "
                + "code 500.");
        Normalize(result.Message!).MatchInlineSnapshot(
            "Nitro at 'https://nitro.test/' returned no fusion configuration for the api id "
            + "'QXBpCmc1YzhkY2Uz' and the stage 'dev'.");
    }

    [Fact]
    public async Task GetSeedAsync_Should_KeepEntriesApart_When_TheStageDiffers()
    {
        // arrange
        var provider = CreateProvider();
        var connection = CreateConnection(NitroCredential.FromApiKey("nitro-api-key"));
        await ServeArchiveAsync("dev-products");
        var dev = await provider.GetSeedAsync(
            connection,
            ApiId,
            "dev",
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // act
        await ServeArchiveAsync("prod-products");
        var prod = await provider.GetSeedAsync(
            connection,
            ApiId,
            "prod",
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.NotEqual(dev.FilePath, prod.FilePath);
        Assert.Equal(
            ["dev-products"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                dev.FilePath!,
                TestContext.Current.CancellationToken));
        Assert.Equal(
            ["prod-products"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                prod.FilePath!,
                TestContext.Current.CancellationToken));
    }

    private static NitroCredential CreateExpiredCredential()
        => NitroCredential.Unavailable(
            NitroCredentialUnavailableReason.SessionExpired,
            "The Nitro session stored at '/tmp/session.json' expired at 2026-07-29 11:00:00Z. "
            + "Run 'nitro login' to sign in again.");

    private static string GetFallbackWarning(RecordingLogger<NitroSeedProviderTests> logger)
        => Assert.Single(
                logger.Entries,
                entry => entry.Message.StartsWith(
                    "A fresh fusion configuration could NOT be fetched",
                    StringComparison.Ordinal))
            .Message;

    private string Normalize(string value)
        => value.Replace(
            _server.BaseAddress.AbsoluteUri,
            ApiUrlPlaceholder,
            StringComparison.Ordinal);

    private async Task ServeArchiveAsync(params string[] sourceSchemaNames)
    {
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            sourceSchemaNames);

        _server.DownloadHandler = _ => FakeNitroResponse.Archive(archive);
    }

    private async Task<NitroSeedProvider> SeedTheCacheAsync()
    {
        var provider = CreateProvider();
        await ServeArchiveAsync("products");

        var result = await provider.GetSeedAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            ApiId,
            Stage,
            new RecordingLogger<NitroSeedProviderTests>(),
            TestContext.Current.CancellationToken);

        Assert.Equal(NitroSeedOutcome.Downloaded, result.Outcome);

        return provider;
    }

    private NitroSeedProvider CreateProvider() => CreateProvider(attemptsWithoutCachedSeed: 1);

    private NitroSeedProvider CreateProvider(int attemptsWithoutCachedSeed)
        => new(
            new NitroFusionConfigurationDownloader(
                _httpClient,
                new NitroDownloadRetryPolicy(
                    attemptsWithCachedSeed: 2,
                    attemptsWithoutCachedSeed,
                    TimeSpan.Zero),
                TimeProvider.System),
            new NitroSeedCache(_directory.GetPath("cache"), _timeProvider),
            new NitroApiLookupClient(
                GraphQLHttpClient.Create(_httpClient, disposeHttpClient: false)));

    private NitroConnection CreateConnection(NitroCredential credential)
    {
        NitroApiUrl.TryNormalize(_server.BaseAddress.AbsoluteUri, out var apiUrl);

        return new NitroConnection(
            apiUrl!,
            NitroApiUrl.CreateGraphQLEndpoint(apiUrl!),
            credential);
    }
}
