using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
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
            host.Dispose();
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
            var content = new StringContent(
                """{ "id": "6", "name": "Test", "email": "test@example.com" }""",
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync("/users/6", content, cancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
            host.Dispose();
        }
    }

    private async Task<IWebHost> StartKestrelHostAsync(
        IOpenApiDefinitionStorage storage,
        CancellationToken cancellationToken)
    {
        var host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://127.0.0.1:0")
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.AddGraphQLServer()
                    .AddOpenApi()
                    .AddOpenApiDefinitionStorage(storage)
                    .AddBasicServer();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapOpenApiEndpoints();
                    endpoints.MapGraphQL();
                });
            })
            .Build();

        await host.StartAsync(cancellationToken);
        return host;
    }

    private static Uri GetBaseAddress(IWebHost host)
    {
        var addresses = host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses
            ?? throw new InvalidOperationException("Kestrel did not expose any server addresses.");

        var address = addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("Kestrel did not bind to any address.");

        return new Uri(address);
    }
}
