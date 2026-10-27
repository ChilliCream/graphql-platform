using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion;

public class SourceSchemaClientReuseTests : FusionTestBase
{
    private const string ForwardedHeader = "X-Forwarded-Test";
    private const string HandlerIdHeader = "X-Handler-Id";

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

    [Fact]
    public async Task Execute_Should_UseCurrentHandler_When_SharedClientExpiredWhileIdle()
    {
        // arrange
        var handlerCount = 0;
        using var server = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Query>(),
            configureServices: s => s.AddHttpContextAccessor());
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: services =>
            {
                services.RemoveAll<IHttpClientFactory>();
                services
                    .AddHttpClient("A")
                    .SetHandlerLifetime(TimeSpan.FromSeconds(1))
                    .ConfigurePrimaryHttpMessageHandler(() => server.CreateHandler())
                    .AddHttpMessageHandler(() => new HandlerIdHandler(Interlocked.Increment(ref handlerCount)));
            },
            includeOperationPlan: false);
        using var client = gateway.CreateClient();
        var first = await ExecuteAsync(client, "{ handlerId }");
        await WaitForHandlerRotationAsync(gateway.Services.GetRequiredService<IHttpClientFactory>());

        // act
        var second = await ExecuteAsync(client, "{ handlerId }");

        // assert
        new[] { first, second }.MatchInlineSnapshots(
        [
            """{"data":{"handlerId":"1"}}""",
            """{"data":{"handlerId":"2"}}"""
        ]);
    }

    [Fact]
    public async Task Execute_Should_ApplyReloadedHttpClientOptions_When_NamedOptionsChange()
    {
        // arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Authorize"] = "false" })
            .Build();
        using var server = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Query>(),
            configureServices: s => s.AddHttpContextAccessor());
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: services =>
            {
                services.RemoveAll<IHttpClientFactory>();
                services
                    .AddHttpClient("A")
                    .ConfigurePrimaryHttpMessageHandler(() => server.CreateHandler());
                services.Configure<HttpClientFactoryOptions>("A", options =>
                {
                    if (configuration["Authorize"] == "true")
                    {
                        options.HttpClientActions.Add(c =>
                            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token"));
                    }
                });
                services.AddSingleton<IOptionsChangeTokenSource<HttpClientFactoryOptions>>(
                    new ConfigurationChangeTokenSource<HttpClientFactoryOptions>("A", configuration));
            },
            includeOperationPlan: false);
        using var client = gateway.CreateClient();
        var before = await ExecuteAsync(client, "{ authorization }");

        // act
        configuration["Authorize"] = "true";
        configuration.Reload();
        var after = await ExecuteAsync(client, "{ authorization }");

        // assert
        new[] { before, after }.MatchInlineSnapshots(
        [
            """{"data":{"authorization":"none"}}""",
            """{"data":{"authorization":"Bearer token"}}"""
        ]);
    }

    private static async Task<string> ExecuteAsync(HttpClient client, string query)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = new StringContent(
            $$"""{ "query": "{{query}}" }""",
            Encoding.UTF8,
            "application/json");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    // The factory replaces the handler on its own timer, so we wait until a new client uses the next one.
    private static async Task WaitForHandlerRotationAsync(IHttpClientFactory factory)
    {
        var start = Stopwatch.GetTimestamp();

        while (Stopwatch.GetElapsedTime(start) < TimeSpan.FromSeconds(15))
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);

            using var client = factory.CreateClient("A");
            client.BaseAddress = new Uri("http://localhost:5000");

            if (await ExecuteAsync(client, "{ handlerId }") != """{"data":{"handlerId":"1"}}""")
            {
                return;
            }
        }

        throw new TimeoutException("The HTTP client handler was not replaced.");
    }

    public sealed class Query
    {
        public string? GetForwardedHeader([Service] IHttpContextAccessor httpContextAccessor)
            => httpContextAccessor.HttpContext?.Request.Headers[ForwardedHeader].ToString();

        public string? GetHandlerId([Service] IHttpContextAccessor httpContextAccessor)
            => httpContextAccessor.HttpContext?.Request.Headers[HandlerIdHeader].ToString();

        public string GetAuthorization([Service] IHttpContextAccessor httpContextAccessor)
            => httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString() is { Length: > 0 } value
                ? value
                : "none";
    }

    private sealed class HandlerIdHandler(int id) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Add(HandlerIdHeader, id.ToString());
            return base.SendAsync(request, cancellationToken);
        }
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
