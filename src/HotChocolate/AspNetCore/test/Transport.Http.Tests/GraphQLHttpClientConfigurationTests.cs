using System.Net;
using static HotChocolate.AspNetCore.Tests.Utilities.TestServerExtensions;

namespace HotChocolate.Transport.Http.Tests;

public class GraphQLHttpClientConfigurationTests
{
    [Fact]
    public async Task DefaultRequestVersion()
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler(
            request =>
            {
                Assert.Equal(HttpVersion.Version20, request.Version);
                Assert.Equal(HttpVersionPolicy.RequestVersionOrHigher, request.VersionPolicy);
                return new();
            }
        ))
        {
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        };

        var client = GraphQLHttpClient.Create(httpClient, true);
        await client.SendAsync(new("{ __typename }", new(CreateUrl(default))), default);
    }

    internal class TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> sender) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(sender.Invoke(request));
        }
    }
}
