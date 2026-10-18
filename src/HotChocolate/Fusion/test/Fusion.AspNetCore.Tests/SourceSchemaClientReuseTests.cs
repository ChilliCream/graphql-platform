using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion;

public class SourceSchemaClientReuseTests : FusionTestBase
{
    private const string ForwardedHeader = "X-Forwarded-Test";

    [Fact]
    public async Task Execute_Should_ForwardHeaderOfEachRequest_When_DefaultHttpClientFactoryIsUsed()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Query>(),
            configureServices: s => s.AddHttpContextAccessor());
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: services =>
            {
                services.RemoveAll<IHttpClientFactory>();
                services.AddHttpContextAccessor();
                services
                    .AddHttpClient("A")
                    .ConfigurePrimaryHttpMessageHandler(() => server.CreateHandler())
                    .AddHttpMessageHandler(
                        sp => new ForwardHeaderHandler(sp.GetRequiredService<IHttpContextAccessor>()));
            },
            includeOperationPlan: false);
        using var client = gateway.CreateClient();

        // act
        var results = new List<string>();

        foreach (var value in new[] { "first", "second", "third" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
            request.Headers.Add(ForwardedHeader, value);
            request.Content = new StringContent(
                """{ "query": "{ forwardedHeader }" }""",
                Encoding.UTF8,
                "application/json");
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            results.Add(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        // assert
        results.MatchInlineSnapshots(
        [
            """{"data":{"forwardedHeader":"first"}}""",
            """{"data":{"forwardedHeader":"second"}}""",
            """{"data":{"forwardedHeader":"third"}}"""
        ]);
    }

    public sealed class Query
    {
        public string? GetForwardedHeader([Service] IHttpContextAccessor httpContextAccessor)
            => httpContextAccessor.HttpContext?.Request.Headers[ForwardedHeader].ToString();
    }

    private sealed class ForwardHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (httpContextAccessor.HttpContext?.Request.Headers[ForwardedHeader] is { Count: > 0 } value)
            {
                request.Headers.Add(ForwardedHeader, value.ToString());
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
