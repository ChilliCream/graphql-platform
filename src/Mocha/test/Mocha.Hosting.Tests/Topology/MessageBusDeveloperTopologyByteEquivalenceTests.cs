using System.Text;
using System.Text.Json;
using CookieCrumble;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.InMemory;

namespace Mocha.Hosting.Tests.Topology;

#pragma warning disable CS0618 // Type or member is obsolete — these tests intentionally exercise the deprecated bridge.

/// <summary>
/// Asserts the legacy <c>MapMessageBusDeveloperTopology</c> JSON shape stays byte-equivalent
/// against a frozen snapshot for a deterministic test app. Acceptance criterion per plan §9 —
/// "calling the [Obsolete] MapMessageBusDeveloperTopology in release N still returns the legacy
/// JSON shape, byte-equivalent for a fixed test app".
/// </summary>
/// <remarks>
/// The deterministic test app uses a fixed <see cref="HostDescription.InstanceId"/> and a single
/// known event handler. Runtime collections backed by <see cref="System.Collections.Immutable.ImmutableHashSet{T}"/>
/// iterate in a process-randomised order, so the captured JSON is canonicalised — every JSON array
/// of objects is sorted by the canonical JSON string of its elements before snapshot matching.
/// That makes the snapshot stable across processes while still asserting the structural and
/// byte-level shape of every individual element.
/// </remarks>
public sealed class MessageBusDeveloperTopologyByteEquivalenceTests
{
    private static readonly Guid s_fixedInstanceId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task LegacyBridge_Should_ReturnFrozenJson_When_FixedTestApp()
    {
        // arrange
        using var host = await CreateHost();
        using var client = host.GetTestClient();

        // act
        using var response = await client.GetAsync("/topology");

        // assert — byte-equivalent against the committed snapshot, after canonicalising array order.
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(default);
        var canonical = CanonicaliseJson(json);
        canonical.MatchSnapshot();
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

        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("services", out var services));
        Assert.Equal(JsonValueKind.Array, services.ValueKind);
        Assert.True(services.GetArrayLength() > 0);

        var firstService = services[0];
        Assert.True(firstService.TryGetProperty("host", out var hostElement));
        Assert.Equal(s_fixedInstanceId.ToString("D"), hostElement.GetProperty("instanceId").GetString());
        Assert.Equal("byte-equivalence-test", hostElement.GetProperty("serviceName").GetString());

        Assert.True(firstService.TryGetProperty("messageTypes", out var messageTypes));
        Assert.Equal(JsonValueKind.Array, messageTypes.ValueKind);

        Assert.True(firstService.TryGetProperty("consumers", out var consumers));
        Assert.Equal(JsonValueKind.Array, consumers.ValueKind);
        Assert.True(consumers.GetArrayLength() > 0);

        Assert.True(firstService.TryGetProperty("routes", out var routes));
        Assert.True(routes.TryGetProperty("inbound", out _));
        Assert.True(routes.TryGetProperty("outbound", out _));

        Assert.True(doc.RootElement.TryGetProperty("transports", out var transports));
        Assert.Equal(JsonValueKind.Array, transports.ValueKind);
        Assert.True(transports.GetArrayLength() > 0);
    }

    private static string CanonicaliseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            WriteCanonical(doc.RootElement, writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                var items = element.EnumerateArray()
                    .Select(static item => (Element: item, Sort: SerialiseForSort(item)))
                    .OrderBy(static pair => pair.Sort, StringComparer.Ordinal)
                    .ToList();
                foreach (var (item, _) in items)
                {
                    WriteCanonical(item, writer);
                }

                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string SerialiseForSort(JsonElement element)
    {
        // Compact sort key derived from the element's content — stable across processes.
        var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            element.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
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
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapMessageBusDeveloperTopology("/topology"));
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
}

#pragma warning restore CS0618
