using System.Net;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Schema;

public sealed class NitroApiServiceTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Authentication failed")]
    [InlineData(HttpStatusCode.Forbidden, "Access denied")]
    [InlineData(HttpStatusCode.NotFound, "No published schema found")]
    [InlineData(HttpStatusCode.InternalServerError, "server error")]
    public async Task DownloadSchemaAsync_Maps_HttpErrors_Correctly(
        HttpStatusCode statusCode, string expectedMessageFragment)
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("")
            });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        var factory = new FakeHttpClientFactory(httpClient);
        var service = new NitroApiService(factory);

        var result = await service.DownloadSchemaAsync(
            "test-api", "production", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(expectedMessageFragment, result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadSchemaAsync_NotFound_Includes_Stage_And_ApiId()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("")
            });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        var factory = new FakeHttpClientFactory(httpClient);
        var service = new NitroApiService(factory);

        var result = await service.DownloadSchemaAsync(
            "my-api-id", "staging", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("staging", result.ErrorMessage);
        Assert.Contains("my-api-id", result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadSchemaAsync_Success_Returns_Sdl()
    {
        const string expectedSdl = "type Query { hello: String }";
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedSdl)
            });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        var factory = new FakeHttpClientFactory(httpClient);
        var service = new NitroApiService(factory);

        var result = await service.DownloadSchemaAsync(
            "test-api", "production", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedSdl, result.Sdl);
    }

    [Fact]
    public async Task DownloadSchemaAsync_UrlEncodes_ApiId_And_Stage()
    {
        var handler = new CapturingHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("type Query { a: String }")
            });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        var factory = new FakeHttpClientFactory(httpClient);
        var service = new NitroApiService(factory);

        await service.DownloadSchemaAsync(
            "api/with spaces", "stage+name", CancellationToken.None);

        var requestUri = handler.CapturedRequestUri!.AbsoluteUri;
        Assert.Contains("api%2Fwith%20spaces", requestUri);
        Assert.Contains("stage%2Bname", requestUri);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public CapturingHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public Uri? CapturedRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedRequestUri = request.RequestUri;
            return Task.FromResult(_response);
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }
}
