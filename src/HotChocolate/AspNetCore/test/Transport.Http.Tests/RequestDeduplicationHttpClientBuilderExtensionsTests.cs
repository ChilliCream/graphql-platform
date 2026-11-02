using System.Net;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Transport.Http;

public class RequestDeduplicationHttpClientBuilderExtensionsTests
{
    private const string TenantHeader = "X-Tenant-Id";
    private static readonly HttpRequestOptionsKey<string> s_tenant = new("Tenant");

    [Theory]
    [InlineData("tenant-a", "tenant-b", 2)]
    [InlineData("tenant-a", "tenant-a", 1)]
    public async Task AddRequestDeduplication_Should_IncludeHashHeaders_When_LaterHandlerSetsThem(
        string firstTenant,
        string secondTenant,
        int expectedRequestCount)
    {
        // arrange
        var requestCount = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var services = new ServiceCollection();
        services
            .AddHttpClient("test")
            .AddRequestDeduplication(options => options.HashHeaders = [TenantHeader])
            .AddHttpMessageHandler(() => new TenantHeaderHandler())
            .ConfigurePrimaryHttpMessageHandler(() => new GatedHandler(async ct =>
            {
                Interlocked.Increment(ref requestCount);
                await gate.Task.WaitAsync(ct);
            }));

        await using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("test");

        using var request1 = CreateQueryRequest(firstTenant);
        using var request2 = CreateQueryRequest(secondTenant);

        // act
        var task1 = client.SendAsync(request1, TestContext.Current.CancellationToken);
        var task2 = client.SendAsync(request2, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        gate.SetResult();

        using var response1 = await task1;
        using var response2 = await task2;

        // assert
        Assert.Equal(expectedRequestCount, requestCount);
    }

    private static HttpRequestMessage CreateQueryRequest(string tenant)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/graphql")
        {
            Content = new StringContent("{\"query\":\"{ hello }\"}")
        };
        request.Options.Set(GraphQLHttpRequest.OperationKindOptionsKey, OperationType.Query);
        request.Options.Set(s_tenant, tenant);

        return request;
    }

    private sealed class TenantHeaderHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Options.TryGetValue(s_tenant, out var tenant))
            {
                request.Headers.Add(TenantHeader, tenant);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class GatedHandler(Func<CancellationToken, Task> onRequest) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await onRequest(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        }
    }
}
