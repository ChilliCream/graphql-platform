using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Regression tests for OpenAPI endpoints that bind the request body via
/// <c>@body</c> (<c>POST</c>/<c>PUT</c>/<c>PATCH</c>). These run against a real
/// Kestrel host rather than <see cref="Microsoft.AspNetCore.TestHost.TestServer"/>.
/// <para>
/// <b>Problem.</b> Every <c>@body</c> endpoint returned an empty
/// <c>500 Internal Server Error</c> when hosted on Kestrel.
/// <see cref="DynamicEndpointMiddleware"/>.<c>BuildVariablesAsync</c> read the
/// request body in a loop that called <c>PipeReader.AdvanceTo</c> on every
/// iteration, and then called <c>AdvanceTo</c> a second time after the loop with
/// no intervening <c>ReadAsync</c>. That violates the <c>PipeReader</c> contract,
/// so Kestrel's request body reader throws
/// <c>InvalidOperationException: "No reading operation to complete."</c>, which the
/// middleware's catch-all turns into a bare 500 (nothing logged).
/// </para>
/// <para>
/// <b>Why the existing <c>TestServer</c> tests miss it.</b>
/// <c>TestServer</c>'s in-memory request-body <c>PipeReader</c> tolerates the
/// redundant <c>AdvanceTo</c>; only a real Kestrel host enforces the contract and
/// throws. The fix restructures the loop so <c>AdvanceTo</c> runs only while more
/// data is pending and then exactly once after parsing the completed buffer.
/// </para>
/// <para>
/// <b>Reproduce (before the fix).</b> Host any GraphQL operation with a
/// <c>@body</c> variable over POST on Kestrel and call it with a JSON body, e.g.:
/// <code>
/// curl -i -X POST http://127.0.0.1:5000/users \
///   -H "Content-Type: application/json" \
///   -d '{"id":"6","name":"Test","email":"test@example.com"}'
/// # before: HTTP/1.1 500 (empty body)   after: HTTP/1.1 200 with the mapped JSON
/// </code>
/// </para>
/// </summary>
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
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var storage = CreateBasicTestDefinitionStorage();

        var host = await StartKestrelHostAsync(storage, cancellationToken);

        try
        {
            using var client = new HttpClient { BaseAddress = GetBaseAddress(host) };
            var content = new StringContent(
                """
                {
                  "id": "6",
                  "name": "Test",
                  "email": "test@example.com"
                }
                """,
                Encoding.UTF8,
                "application/json");

            // act
            var response = await client.PostAsync("/users", content, cancellationToken);

            // assert
            // Before the fix this was 500 (empty body) because of the double
            // PipeReader.AdvanceTo described in the class remarks.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // The body should be the mapped GraphQL response (the "createUser" root
            // field), i.e. a JSON object rather than an empty 500 body.
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
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var storage = CreateBasicTestDefinitionStorage();

        var host = await StartKestrelHostAsync(storage, cancellationToken);

        try
        {
            using var client = new HttpClient { BaseAddress = GetBaseAddress(host) };
            var content = new StringContent(
                """
                {
                  "id": "6",
                  "name": "Test",
                  "email": "test@example.com"
                }
                """,
                Encoding.UTF8,
                "application/json");

            // act
            // The PUT route ("/users/{userId:$user.id}") also reads the request
            // body through the same code path, so it regressed identically.
            var response = await client.PutAsync("/users/6", content, cancellationToken);

            // assert
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
            // Bind to a free port chosen by the OS.
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
