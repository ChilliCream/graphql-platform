using System.Net;

namespace HotChocolate.Fusion;

public sealed class DefaultSchemaFetcherTests
{
    [Fact]
    public async Task FetchAsync_Should_ReturnSchemaDocument_When_ResponseIsValid()
    {
        HttpMethod? method = null;
        Uri? requestUri = null;
        using var client = CreateClient(request =>
        {
            method = request.Method;
            requestUri = request.RequestUri;
            return Task.FromResult(
                Response(HttpStatusCode.OK, "type Query { hello: String }"));
        });

        var result = await DefaultSchemaFetcher.FetchAsync(
            client,
            "Products",
            new Uri("https://products.example.com/graphql/schema.graphql"),
            TestContext.Current.CancellationToken);

        Assert.Equal("type Query { hello: String }", result);
        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal(
            new Uri("https://products.example.com/graphql/schema.graphql"),
            requestUri);
    }

    [Fact]
    public async Task FetchAsync_Should_ThrowSourceError_When_HttpRequestFails()
    {
        using var client = CreateClient(
            _ => Task.FromResult(Response(HttpStatusCode.ServiceUnavailable, "unavailable")));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DefaultSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://products.example.com/graphql/schema.graphql"),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Source schema 'Products' returned HTTP 503 (Service Unavailable) while downloading its schema.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FetchAsync_Should_ThrowSourceError_When_ResponseIsEmpty(string responseBody)
    {
        using var client = CreateClient(
            _ => Task.FromResult(Response(HttpStatusCode.OK, responseBody)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DefaultSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://products.example.com/graphql/schema.graphql"),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Source schema 'Products' returned an empty schema response.",
            exception.Message);
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
            () => DefaultSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://user:secret@products.example.com/schema?token=secret"),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Source schema 'Products' returned a response larger than the maximum allowed size "
            + "of 50000000 bytes.",
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
            () => DefaultSchemaFetcher.FetchAsync(
                client,
                "Products",
                new Uri("https://products.example.com/graphql/schema.graphql"),
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
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
