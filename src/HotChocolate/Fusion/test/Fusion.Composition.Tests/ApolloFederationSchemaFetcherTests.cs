using System.Net;

namespace HotChocolate.Fusion;

public sealed class ApolloFederationSchemaFetcherTests
{
    [Fact]
    public async Task FetchAsync_Should_ReturnServiceSdl_When_ResponseIsValid()
    {
        HttpMethod? method = null;
        Uri? requestUri = null;
        string? mediaType = null;
        string? requestBody = null;
        using var client = CreateClient(async request =>
        {
            method = request.Method;
            requestUri = request.RequestUri;
            mediaType = request.Content?.Headers.ContentType?.MediaType;
            requestBody = await request.Content!.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

            return Response(
                HttpStatusCode.OK,
                """
                {
                  "data": {
                    "_service": {
                      "sdl": "type Query { hello: String }"
                    }
                  }
                }
                """);
        });

        var result = await ApolloFederationSchemaFetcher.FetchAsync(
            client,
            "Products",
            new Uri("https://products.example.com/graphql"),
            TestContext.Current.CancellationToken);

        Assert.Equal("type Query { hello: String }", result);
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal(new Uri("https://products.example.com/graphql"), requestUri);
        Assert.Equal("application/json", mediaType);
        Assert.Equal(
            """{"query":"query FusionServiceSdl { _service { sdl } }"}""",
            requestBody);
    }

    [Fact]
    public async Task FetchAsync_Should_ThrowSourceError_When_HttpRequestFails()
    {
        using var client = CreateClient(
            _ => Task.FromResult(Response(HttpStatusCode.ServiceUnavailable, "unavailable")));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ApolloFederationSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://products.example.com/graphql"),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Source schema 'Products' returned HTTP 503 (Service Unavailable) for the _service query.",
            exception.Message);
    }

    [Fact]
    public async Task FetchAsync_Should_ThrowSourceError_When_ResponseContainsGraphQLErrors()
    {
        using var client = CreateClient(
            _ => Task.FromResult(
                Response(
                    HttpStatusCode.OK,
                    """
                    {
                      "errors": [
                        { "message": "The service field is disabled." },
                        { "message": "Contact the service owner." }
                      ]
                    }
                    """)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ApolloFederationSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://products.example.com/graphql"),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Source schema 'Products' rejected the _service query: "
            + "The service field is disabled.; Contact the service owner.",
            exception.Message);
    }

    [Fact]
    public async Task FetchAsync_Should_PreserveCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var client = CreateClient(
            _ => Task.FromCanceled<HttpResponseMessage>(cancellation.Token));

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(
            () => ApolloFederationSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://products.example.com/graphql"),
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task FetchAsync_Should_RejectResponse_When_ResponseExceedsSchemaSizeLimit()
    {
        using var client = CreateClient(
            _ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([])
                };
                response.Content.Headers.ContentLength = SchemaHttpResponseReader.MaxResponseSize + 1;
                return Task.FromResult(response);
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ApolloFederationSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://user:secret@products.example.com/graphql?token=secret"),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Source schema 'Products' returned a response larger than the maximum allowed size "
            + "of 50000000 bytes.",
            exception.Message);
    }

    [Theory]
    [InlineData("", "returned an empty _service response.")]
    [InlineData("not-json", "returned an invalid _service response.")]
    [InlineData("[]", "returned an invalid _service response.")]
    [InlineData("{}", "returned no _service.sdl.")]
    [InlineData("{\"errors\":[{}]}", "returned an invalid _service response.")]
    [InlineData("{\"errors\":{}}", "returned an invalid _service response.")]
    [InlineData("{\"data\":{\"_service\":{\"sdl\":null}}}", "returned no _service.sdl.")]
    [InlineData("{\"data\":{\"_service\":{\"sdl\":\"\"}}}", "returned no _service.sdl.")]
    [InlineData("{\"data\":{\"_service\":{\"sdl\":\"   \"}}}", "returned no _service.sdl.")]
    public async Task FetchAsync_Should_ThrowSourceError_When_ResponseIsInvalid(
        string responseBody,
        string expectedMessage)
    {
        using var client = CreateClient(
            _ => Task.FromResult(Response(HttpStatusCode.OK, responseBody)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ApolloFederationSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://products.example.com/graphql"),
                TestContext.Current.CancellationToken));

        Assert.Equal($"Source schema 'Products' {expectedMessage}", exception.Message);
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        => new(new StubHttpMessageHandler(responseFactory));

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string content)
        => new(statusCode) { Content = new StringContent(content) };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responseFactory(request);
    }
}
