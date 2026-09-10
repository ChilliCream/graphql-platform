using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroApiLookupClientTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient = new();
    private FakeNitroServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FakeNitroServer.StartAsync();

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task ResolveApiAsync_Should_SendTheOperation_When_ItLooksUpAnApi()
    {
        // arrange
        _server.GraphQLHandler = _ =>
            FakeNitroResponse.Json("""{"data":{"node":{"name":"Demo Api"}}}""");
        var client = CreateClient();

        // act
        await client.ResolveApiAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            new RecordingLogger<NitroApiLookupClientTests>(),
            TestContext.Current.CancellationToken);

        // assert
        var request = Assert.Single(_server.Requests);

        Assert.Equal("/graphql", request.Path);
        Assert.Equal("nitro-api-key", request.Headers[NitroRequestHeaders.ApiKey]);
#if !NITRO_PERSISTED_OPERATIONS
        request.Body.MatchInlineSnapshot(
            """
            {"query":"query ResolveNitroApiName($id: ID!) {\n  node(id: $id) {\n    ... on Api {\n      name\n    }\n  }\n}\n","operationName":"ResolveNitroApiName","variables":{"id":"api-1"}}
            """);
#else
        request.Body.MatchInlineSnapshot(
            """
            {"id":"6480d908b5fe1cad49198376e03c5547c83234d90c154c9566324717cda41f45","operationName":"ResolveNitroApiName","variables":{"id":"api-1"}}
            """);
#endif
    }

    [Fact]
    public async Task ResolveApiAsync_Should_ReportFound_When_TheNodeIsAnApi()
    {
        // arrange
        _server.GraphQLHandler = _ =>
            FakeNitroResponse.Json("""{"data":{"node":{"name":"Demo Api"}}}""");
        var client = CreateClient();

        // act
        var result = await client.ResolveApiAsync(
            CreateConnection(NitroCredential.FromAccessToken("access-token")),
            "api-1",
            new RecordingLogger<NitroApiLookupClientTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroApiLookupStatus.Found, result.Status);
        Assert.Equal("Demo Api", result.Name);
    }

    [Fact]
    public async Task ResolveApiAsync_Should_ReportNotFound_When_TheNodeIsNull()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json("""{"data":{"node":null}}""");
        var client = CreateClient();

        // act
        var result = await client.ResolveApiAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            new RecordingLogger<NitroApiLookupClientTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroApiLookupStatus.NotFound, result.Status);
        Assert.Null(result.Name);
    }

    [Fact]
    public async Task ResolveApiAsync_Should_ReportFailedAndWarn_When_TheResponseCarriesErrors()
    {
        // arrange
        _server.GraphQLHandler = _ =>
            FakeNitroResponse.Json("""{"errors":[{"message":"The current user is not authorized."}]}""");
        var client = CreateClient();
        var logger = new RecordingLogger<NitroApiLookupClientTests>();

        // act
        var result = await client.ResolveApiAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            logger,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroApiLookupStatus.Failed, result.Status);
        Assert.Equal(
            "The Nitro api lookup for the api id api-1 returned errors: "
            + """[{"message":"The current user is not authorized."}]""",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task ResolveApiAsync_Should_ReportFailed_When_TheServerReturnsAnErrorStatus()
    {
        // arrange
        _server.GraphQLHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status500InternalServerError);
        var client = CreateClient();

        // act
        var result = await client.ResolveApiAsync(
            CreateConnection(NitroCredential.FromApiKey("nitro-api-key")),
            "api-1",
            new RecordingLogger<NitroApiLookupClientTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroApiLookupStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ResolveApiAsync_Should_ReportFailed_When_TheServerCannotBeReached()
    {
        // arrange
        // port 1 refuses the connection, which surfaces as a connection level failure
        var apiUrl = new Uri("http://127.0.0.1:1/");
        var connection = new NitroConnection(
            apiUrl,
            NitroApiUrl.CreateGraphQLEndpoint(apiUrl),
            NitroCredential.FromApiKey("nitro-api-key"));
        var client = CreateClient();

        // act
        var result = await client.ResolveApiAsync(
            connection,
            "api-1",
            new RecordingLogger<NitroApiLookupClientTests>(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NitroApiLookupStatus.Failed, result.Status);
    }

    private NitroApiLookupClient CreateClient()
        => new(GraphQLHttpClient.Create(_httpClient, disposeHttpClient: false));

    private NitroConnection CreateConnection(NitroCredential credential)
    {
        NitroApiUrl.TryNormalize(_server.BaseAddress.AbsoluteUri, out var apiUrl);

        return new NitroConnection(
            apiUrl!,
            NitroApiUrl.CreateGraphQLEndpoint(apiUrl!),
            credential);
    }
}
