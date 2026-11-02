using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

// Regression tests for @body endpoints. They must run on a real Kestrel host:
// TestServer's in-memory body PipeReader tolerates the double AdvanceTo bug,
// only Kestrel enforces the contract and returns 500.
public sealed class KestrelHttpEndpointIntegrationTests : OpenApiTestBase
{
    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        OpenApiDiagnosticEventListener? eventListener)
    {
        services.AddGraphQLServer()
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();
    }

    [Fact]
    public async Task Http_Post_Body_Succeeds_On_Kestrel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var storage = CreateBasicTestDefinitionStorage();
        var host = await StartKestrelHostAsync(storage, cancellationToken);

        try
        {
            using var client = new HttpClient { BaseAddress = GetBaseAddress(host) };
            var content = new StringContent(
                """{ "id": "6", "name": "Test", "email": "test@example.com" }""",
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/users", content, cancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(JsonValueKind.Object, json.RootElement.ValueKind);
            Assert.True(json.RootElement.TryGetProperty("name", out _));
        }
        finally
        {
            await host.StopAsync(cancellationToken);
            await host.DisposeAsync();
        }
    }

    [Fact]
    public async Task Http_Put_Body_Succeeds_On_Kestrel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var storage = CreateBasicTestDefinitionStorage();
        var host = await StartKestrelHostAsync(storage, cancellationToken);

        try
        {
            using var client = new HttpClient { BaseAddress = GetBaseAddress(host) };
            // The PUT route is "/users/{userId:$user.id}", so "id" is supplied by the
            // route segment and must not also appear in the body.
            var content = new StringContent(
                """{ "name": "Test", "email": "test@example.com" }""",
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync("/users/6", content, cancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
            await host.DisposeAsync();
        }
    }

    private async Task<WebApplication> StartKestrelHostAsync(
        IOpenApiDefinitionStorage storage,
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddRouting();
        builder.Services
            .AddGraphQLServer()
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();

        var host = builder.Build();
        host.MapOpenApiEndpoints();
        host.MapGraphQL();

        await host.StartAsync(cancellationToken);
        return host;
    }

    private static Uri GetBaseAddress(WebApplication host)
    {
        var address = host.Urls.FirstOrDefault()
            ?? throw new InvalidOperationException("Kestrel did not bind to any address.");

        return new Uri(address);
    }
}
