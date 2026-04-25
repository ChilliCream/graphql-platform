using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Resources;
using Mocha.Transport.InMemory;

namespace Mocha.Hosting.Tests.Topology;

#pragma warning disable CS0618 // Type or member is obsolete — these tests intentionally exercise the deprecated bridge.

/// <summary>
/// Asserts the legacy <c>MapMessageBusDeveloperTopology</c> JSON shape is byte-equivalent for a
/// fixed test app whether the bridge resolves the description from
/// <see cref="MochaMessageBusResourceSource"/> or falls back to running the visitor directly.
/// Acceptance criterion per plan §9 — "calling the [Obsolete] MapMessageBusDeveloperTopology in
/// release N still returns the legacy JSON shape, byte-equivalent for a fixed test app".
/// </summary>
/// <remarks>
/// Both code paths are exercised against the same <see cref="MessagingRuntime"/> so the underlying
/// <c>ImmutableHashSet&lt;Consumer&gt;</c> iteration order is identical for both. Two independent
/// hosts cannot be compared this way because hashset iteration order varies between processes
/// (string hash randomization).
/// </remarks>
public sealed class MessageBusDeveloperTopologyByteEquivalenceTests
{
    private static readonly Guid s_fixedInstanceId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task LegacyBridge_Should_ReturnSameJson_OnBothCodePaths_When_SameRuntime()
    {
        // arrange — single host with the resource source registered, exposing the legacy bridge at
        // /topology and a visitor-only baseline at /baseline-topology. Same runtime backs both.
        using var host = await CreateHost();
        using var client = host.GetTestClient();

        // act
        using var legacyResponse = await client.GetAsync("/topology");
        using var baselineResponse = await client.GetAsync("/baseline-topology");

        legacyResponse.EnsureSuccessStatusCode();
        baselineResponse.EnsureSuccessStatusCode();

        var legacyJson = await legacyResponse.Content.ReadAsStringAsync(default);
        var baselineJson = await baselineResponse.Content.ReadAsStringAsync(default);

        // assert — byte-equivalent
        Assert.Equal(baselineJson, legacyJson);
    }

    [Fact]
    public async Task LegacyBridge_Should_ReturnDiagramDataShape_When_RoutedThroughResourceSource()
    {
        // arrange
        using var host = await CreateHost();
        using var client = host.GetTestClient();

        // act
        using var response = await client.GetAsync("/topology");

        // assert — top-level shape is unchanged: services + transports
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(default);

        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("services", out var services));
        Assert.Equal(System.Text.Json.JsonValueKind.Array, services.ValueKind);
        Assert.True(services.GetArrayLength() > 0);

        var firstService = services[0];
        Assert.True(firstService.TryGetProperty("host", out var hostElement));
        Assert.Equal(s_fixedInstanceId.ToString("D"), hostElement.GetProperty("instanceId").GetString());
        Assert.Equal("byte-equivalence-test", hostElement.GetProperty("serviceName").GetString());

        Assert.True(firstService.TryGetProperty("messageTypes", out var messageTypes));
        Assert.Equal(System.Text.Json.JsonValueKind.Array, messageTypes.ValueKind);

        Assert.True(firstService.TryGetProperty("consumers", out var consumers));
        Assert.Equal(System.Text.Json.JsonValueKind.Array, consumers.ValueKind);
        Assert.True(consumers.GetArrayLength() > 0);

        Assert.True(firstService.TryGetProperty("routes", out var routes));
        Assert.True(routes.TryGetProperty("inbound", out _));
        Assert.True(routes.TryGetProperty("outbound", out _));

        Assert.True(doc.RootElement.TryGetProperty("transports", out var transports));
        Assert.Equal(System.Text.Json.JsonValueKind.Array, transports.ValueKind);
        Assert.True(transports.GetArrayLength() > 0);
    }

    private static async Task<IHost> CreateHost()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();

                    var bus = services.AddMessageBus();
                    bus.AddEventHandler<TestEventHandler>();
                    bus.ConfigureMessageBus(b => ((MessageBusBuilder)b).Host(d =>
                    {
                        d.InstanceId(s_fixedInstanceId);
                        d.ServiceName("byte-equivalence-test");
                        d.AssemblyName("Mocha.Hosting.Tests");
                    }));
                    bus.AddInMemory();

                    services.AddMochaMessageBusResources();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Path 1: the deprecated bridge — routes through MochaMessageBusResourceSource.
                        endpoints.MapMessageBusDeveloperTopology("/topology");

                        // Path 2: a visitor-only baseline serialized with the same options. Acts as
                        // the "before-this-change" reference. Both paths walk the SAME runtime
                        // instance, so ImmutableHashSet ordering is identical and the bytes match.
                        endpoints.MapGet("/baseline-topology", static (HttpContext ctx) =>
                        {
                            var runtime = (MessagingRuntime)ctx.RequestServices.GetRequiredService<IMessagingRuntime>();
                            var description = MessageBusDescriptionVisitor.Visit(runtime);
                            var payload = LegacyDiagramData.Build(description);
                            return Results.Content(
                                System.Text.Json.JsonSerializer.Serialize(payload, LegacyDiagramData.SerializerOptions),
                                "application/json");
                        });
                    });
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    public sealed record TestEvent(string Payload);

    public sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private static class LegacyDiagramData
    {
        public static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase) }
        };

        public static DiagramDataPayload Build(MessageBusDescription description)
        {
            return new DiagramDataPayload(
                [
                    new ServicePayload(
                        description.Host,
                        description.MessageTypes,
                        description.Consumers,
                        description.Routes,
                        description.Sagas ?? [])
                ],
                description.Transports);
        }

        public sealed record DiagramDataPayload(
            IReadOnlyList<ServicePayload> Services,
            IReadOnlyList<TransportDescription> Transports);

        public sealed record ServicePayload(
            HostDescription Host,
            IReadOnlyList<MessageTypeDescription> MessageTypes,
            IReadOnlyList<ConsumerDescription> Consumers,
            RoutesDescription Routes,
            IReadOnlyList<SagaDescription> Sagas);
    }
}

#pragma warning restore CS0618
