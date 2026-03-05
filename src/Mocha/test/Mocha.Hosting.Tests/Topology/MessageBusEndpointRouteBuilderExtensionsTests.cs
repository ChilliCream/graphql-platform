using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.InMemory;

namespace Mocha.Hosting.Tests.Topology;

public sealed class MessageBusEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public async Task MapMessageBus_Should_ReturnJsonTopology_When_DefaultPath()
    {
        // Arrange
        using var host = await CreateHost();
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync("/.well-known/message-topology");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(default);
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task MapMessageBus_Should_ReturnJsonTopology_When_CustomPath()
    {
        // Arrange
        using var host = await CreateHost("/custom/topology");
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync("/custom/topology");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(default);
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task MapMessageBus_Should_ContainHostInfo_When_Called()
    {
        // Arrange
        using var host = await CreateHost();
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync("/.well-known/message-topology");
        var content = await response.Content.ReadAsStringAsync(default);
        var doc = JsonDocument.Parse(content);

        // Assert
        Assert.True(
            doc.RootElement.TryGetProperty("host", out var hostElement),
            "Response JSON should contain a 'host' property.");
        Assert.Equal(JsonValueKind.Object, hostElement.ValueKind);
    }

    private static async Task<IHost> CreateHost(string? topologyPath = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddMessageBus().AddInMemory();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        if (topologyPath is not null)
                        {
                            endpoints.MapMessageBus(topologyPath);
                        }
                        else
                        {
                            endpoints.MapMessageBus();
                        }
                    });
                });
            })
            .Build();

        await host.StartAsync();

        return host;
    }
}
