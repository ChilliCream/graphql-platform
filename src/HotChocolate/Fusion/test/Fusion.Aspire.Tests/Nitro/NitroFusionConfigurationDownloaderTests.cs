using System.Net;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroFusionConfigurationDownloaderTests : IAsyncLifetime
{
    private static readonly string[] s_snapshotHeaders =
    [
        "Authorization",
        NitroRequestHeaders.ApiKey,
        NitroRequestHeaders.Agent,
        NitroRequestHeaders.ClientVersion
    ];

    private readonly HttpClient _httpClient = new();
    private FakeNitroServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FakeNitroServer.StartAsync();

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task DownloadAsync_Should_SendTheNitroRequest_When_TheCredentialIsAnApiKey()
    {
        // arrange
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            "products");
        _server.DownloadHandler = _ => FakeNitroResponse.Archive(archive);
        var downloader = CreateDownloader(attempts: 1);
        await using var destination = new MemoryStream();

        // act
        await downloader.DownloadAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "QXBpCmc1YzhkY2Uz",
            "dev",
            hasCachedSeed: false,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Render(Assert.Single(_server.Requests)).MatchInlineSnapshot(
            """
            GET /api/v1/apis/QXBpCmc1YzhkY2Uz/fusion/configurations/latest/download?stage=dev&format=far&fusionVersion=2.0.0
            CCC-api-key: nitro-api-key
            ccc-agent: HotChocolate.Fusion.Aspire/<version>
            GraphQL-Client-Version: <version>
            """);
    }

    [Fact]
    public async Task DownloadAsync_Should_SendTheNitroRequest_When_TheCredentialIsAnAccessToken()
    {
        // arrange
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            "products");
        _server.DownloadHandler = _ => FakeNitroResponse.Archive(archive);
        var downloader = CreateDownloader(attempts: 1);
        await using var destination = new MemoryStream();

        // act
        await downloader.DownloadAsync(
            CreateConnection(NitroCredential.FromAccessToken("access-token")),
            "QXBpCmc1YzhkY2Uz",
            "prod",
            hasCachedSeed: false,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Render(Assert.Single(_server.Requests)).MatchInlineSnapshot(
            """
            GET /api/v1/apis/QXBpCmc1YzhkY2Uz/fusion/configurations/latest/download?stage=prod&format=far&fusionVersion=2.0.0
            Authorization: Bearer access-token
            ccc-agent: HotChocolate.Fusion.Aspire/<version>
            GraphQL-Client-Version: <version>
            """);
    }

    [Fact]
    public async Task DownloadAsync_Should_WriteThePayload_When_TheServerReturnsTheArchive()
    {
        // arrange
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            "products",
            "reviews");
        _server.DownloadHandler = _ => FakeNitroResponse.Archive(archive);
        var downloader = CreateDownloader(attempts: 1);
        await using var destination = new MemoryStream();

        // act
        var result = await downloader.DownloadAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            "dev",
            hasCachedSeed: false,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroDownloadStatus.Success, result.Status);
        Assert.Equal(archive, destination.ToArray());
    }

    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    public async Task DownloadAsync_Should_ReportUnauthorizedWithoutRetrying_When_ItIsRejected(
        int statusCode)
    {
        // act
        var result = await FailAsync(statusCode);

        // assert
        Assert.Equal(NitroDownloadStatus.Unauthorized, result.Status);
        Assert.Equal(1, result.Attempts);
        Assert.Single(_server.Requests);
    }

    [Fact]
    public async Task DownloadAsync_Should_ReportNotFoundWithoutRetrying_When_ThereIsNoArchive()
    {
        // act
        var result = await FailAsync(StatusCodes.Status404NotFound);

        // assert
        Assert.Equal(NitroDownloadStatus.NotFound, result.Status);
        Assert.Equal(1, result.Attempts);
        Assert.Single(_server.Requests);
    }

    [Fact]
    public async Task DownloadAsync_Should_ReportPermanentWithoutRetrying_When_TheRequestIsBad()
    {
        // act
        var result = await FailAsync(StatusCodes.Status400BadRequest);

        // assert
        Assert.Equal(NitroDownloadStatus.PermanentFailure, result.Status);
        Assert.Equal(1, result.Attempts);
        Assert.Single(_server.Requests);
    }

    [Fact]
    public async Task DownloadAsync_Should_ExhaustTheBudget_When_TheServerKeepsFailing()
    {
        // arrange
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status503ServiceUnavailable);
        var downloader = CreateDownloader(attempts: 3);
        await using var destination = new MemoryStream();

        // act
        var result = await downloader.DownloadAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            "dev",
            hasCachedSeed: false,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroDownloadStatus.TransientExhausted, result.Status);
        Assert.Equal(3, result.Attempts);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal(3, _server.Requests.Count);
    }

    [Fact]
    public async Task DownloadAsync_Should_UseTheShortBudget_When_ACachedSeedExists()
    {
        // arrange
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status500InternalServerError);
        var downloader = new NitroFusionConfigurationDownloader(
            _httpClient,
            new NitroDownloadRetryPolicy(
                attemptsWithCachedSeed: 2,
                attemptsWithoutCachedSeed: 15,
                TimeSpan.Zero),
            TimeProvider.System);
        await using var destination = new MemoryStream();

        // act
        var result = await downloader.DownloadAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            "dev",
            hasCachedSeed: true,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroDownloadStatus.TransientExhausted, result.Status);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(2, _server.Requests.Count);
    }

    [Fact]
    public async Task DownloadAsync_Should_TruncateTheDestination_When_AnAttemptIsRetried()
    {
        // arrange
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            "products");
        var calls = 0;
        _server.DownloadHandler = _ => ++calls == 1
            ? FakeNitroResponse.Status(StatusCodes.Status502BadGateway)
            : FakeNitroResponse.Archive(archive);
        var downloader = CreateDownloader(attempts: 3);
        await using var destination = new MemoryStream();
        await destination.WriteAsync(
            new byte[] { 1, 2, 3, 4 },
            TestContext.Current.CancellationToken);

        // act
        var result = await downloader.DownloadAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            "dev",
            hasCachedSeed: false,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroDownloadStatus.Success, result.Status);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(archive, destination.ToArray());
    }

    [Fact]
    public async Task DownloadAsync_Should_ReportTransient_When_TheServerCannotBeReached()
    {
        // arrange
        // port 1 refuses the connection, which surfaces as a connection level failure
        var apiUrl = new Uri("http://127.0.0.1:1/");
        var connection = new NitroConnection(
            apiUrl,
            NitroApiUrl.CreateGraphQLEndpoint(apiUrl),
            NitroCredential.FromApiKey("nitro-api-key"));
        var downloader = CreateDownloader(attempts: 2);
        await using var destination = new MemoryStream();

        // act
        var result = await downloader.DownloadAsync(
            connection,
            "api-1",
            "dev",
            hasCachedSeed: false,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroDownloadStatus.TransientExhausted, result.Status);
        Assert.Equal(2, result.Attempts);
        Assert.Null(result.StatusCode);
    }

    private async Task<NitroDownloadResult> FailAsync(int statusCode)
    {
        _server.DownloadHandler = _ => FakeNitroResponse.Status(statusCode);

        var downloader = CreateDownloader(attempts: 5);
        await using var destination = new MemoryStream();

        return await downloader.DownloadAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            "dev",
            hasCachedSeed: false,
            destination,
            new RecordingLogger<NitroFusionConfigurationDownloaderTests>(),
            TestContext.Current.CancellationToken);
    }

    private NitroFusionConfigurationDownloader CreateDownloader(int attempts)
        => new(
            _httpClient,
            new NitroDownloadRetryPolicy(attempts, attempts, TimeSpan.Zero),
            TimeProvider.System);

    private NitroConnection CreateConnection(NitroCredential credential)
    {
        NitroApiUrl.TryNormalize(_server.BaseAddress.AbsoluteUri, out var apiUrl);

        return new NitroConnection(
            apiUrl!,
            NitroApiUrl.CreateGraphQLEndpoint(apiUrl!),
            credential);
    }

    private static string Render(RecordedRequest request)
    {
        var lines = new List<string> { $"{request.Method} {request.Path}{request.QueryString}" };

        foreach (var header in s_snapshotHeaders)
        {
            if (request.Headers.TryGetValue(header, out var value))
            {
                lines.Add(
                    $"{header}: "
                    + value.Replace(
                        NitroRequestHeaders.ClientVersionValue,
                        "<version>",
                        StringComparison.Ordinal));
            }
        }

        return string.Join("\n", lines);
    }
}
