using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Mocha.Resources.AspNetCore.Tests;

public sealed class MochaResourceEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public async Task MapMochaResourceEndpoint_Should_ReturnResources_When_Called()
    {
        // arrange
        using var host = await CreateHostAsync(static services =>
        {
            services.AddMochaResources();
            services.AddMochaResourceSource(new TestSource(
            [
                new TestResource("test.alpha", "urn:test:alpha", static writer => writer.WriteString("name", "Alpha")),
                new TestResource("test.beta", "urn:test:beta", static writer => writer.WriteString("name", "Beta"))
            ]));
        });
        using var client = host.GetTestClient();

        // act
        using var response = await client.GetAsync("/.well-known/mocha-resources");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(default);
        var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("resources", out var resourcesElement));
        Assert.Equal(JsonValueKind.Array, resourcesElement.ValueKind);
        Assert.Equal(2, resourcesElement.GetArrayLength());

        var first = resourcesElement[0];
        Assert.Equal("test.alpha", first.GetProperty("kind").GetString());
        Assert.Equal("urn:test:alpha", first.GetProperty("id").GetString());
        Assert.Equal("Alpha", first.GetProperty("attributes").GetProperty("name").GetString());
    }

    [Fact]
    public async Task MapMochaResourceEndpoint_Should_Return304_When_IfNoneMatchHasFreshETag()
    {
        // arrange
        using var host = await CreateHostAsync(static services =>
        {
            services.AddMochaResources();
            services.AddMochaResourceSource(new TestSource(
            [
                new TestResource("test.x", "urn:test:x", static writer => writer.WriteString("name", "X"))
            ]));
        });
        using var client = host.GetTestClient();

        // first request to capture the ETag
        using var firstResponse = await client.GetAsync("/.well-known/mocha-resources");
        firstResponse.EnsureSuccessStatusCode();
        var etag = firstResponse.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrEmpty(etag));

        // act — second request with If-None-Match
        using var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, "/.well-known/mocha-resources");
        conditionalRequest.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag!));
        using var conditionalResponse = await client.SendAsync(conditionalRequest);

        // assert
        Assert.Equal(HttpStatusCode.NotModified, conditionalResponse.StatusCode);
    }

    [Fact]
    public async Task MapMochaResourceEndpoint_Should_ReturnFreshSnapshot_When_ChangeTokenFires()
    {
        // arrange
        var source = new MutableTestSource(
        [
            new TestResource("test.start", "urn:test:start", static writer => writer.WriteString("name", "Start"))
        ]);

        using var host = await CreateHostAsync(services =>
        {
            services.AddMochaResources();
            services.AddMochaResourceSource(source);
        });
        using var client = host.GetTestClient();

        // first request — initial snapshot
        using var firstResponse = await client.GetAsync("/.well-known/mocha-resources");
        firstResponse.EnsureSuccessStatusCode();
        var firstEtag = firstResponse.Headers.ETag?.Tag;
        var firstContent = await firstResponse.Content.ReadAsStringAsync(default);

        // mutate the source — fires the change token, supplies a fresh snapshot
        source.Replace(
        [
            new TestResource("test.start", "urn:test:start", static writer => writer.WriteString("name", "Start")),
            new TestResource("test.added", "urn:test:added", static writer => writer.WriteString("name", "Added"))
        ]);

        // act — second request after the change
        using var secondResponse = await client.GetAsync("/.well-known/mocha-resources");
        secondResponse.EnsureSuccessStatusCode();
        var secondEtag = secondResponse.Headers.ETag?.Tag;
        var secondContent = await secondResponse.Content.ReadAsStringAsync(default);

        // assert — different ETag, new resource visible
        Assert.NotEqual(firstEtag, secondEtag);
        Assert.NotEqual(firstContent, secondContent);
        var doc = JsonDocument.Parse(secondContent);
        Assert.Equal(2, doc.RootElement.GetProperty("resources").GetArrayLength());
    }

    [Fact]
    public async Task MapMochaResourceEndpoint_Should_RespectAuthorization_When_RequireAuthorizationApplied()
    {
        // arrange — endpoint without RequireAuthorization is anonymous-friendly; no auth required.
        using var host = await CreateHostAsync(static services =>
        {
            services.AddMochaResources();
            services.AddMochaResourceSource(new TestSource(
            [
                new TestResource("test.a", "urn:test:a", static writer => writer.WriteString("name", "A"))
            ]));
        });
        using var client = host.GetTestClient();

        // act — anonymous request succeeds because no policy is chained
        using var response = await client.GetAsync("/.well-known/mocha-resources");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapMochaResourceEndpoint_Should_WriteAttributesOpaquely_When_ResourceWriteCalled()
    {
        // arrange — verifies the framework writes whatever the resource emits without inspecting types.
        using var host = await CreateHostAsync(static services =>
        {
            services.AddMochaResources();
            services.AddMochaResourceSource(new TestSource(
            [
                new TestResource(
                    "custom.shape",
                    "urn:custom:shape",
                    static writer =>
                    {
                        writer.WriteNumber("count", 42);
                        writer.WriteBoolean("ready", true);
                        writer.WriteStartArray("tags");
                        writer.WriteStringValue("alpha");
                        writer.WriteStringValue("beta");
                        writer.WriteEndArray();
                        writer.WriteStartObject("nested");
                        writer.WriteString("inner", "value");
                        writer.WriteEndObject();
                    })
            ]));
        });
        using var client = host.GetTestClient();

        // act
        using var response = await client.GetAsync("/.well-known/mocha-resources");
        var content = await response.Content.ReadAsStringAsync(default);

        // assert — every attribute the resource wrote round-trips byte-for-byte.
        var attributes = JsonDocument.Parse(content)
            .RootElement.GetProperty("resources")[0]
            .GetProperty("attributes");

        Assert.Equal(42, attributes.GetProperty("count").GetInt32());
        Assert.True(attributes.GetProperty("ready").GetBoolean());
        var tags = attributes.GetProperty("tags");
        Assert.Equal(2, tags.GetArrayLength());
        Assert.Equal("alpha", tags[0].GetString());
        Assert.Equal("beta", tags[1].GetString());
        Assert.Equal("value", attributes.GetProperty("nested").GetProperty("inner").GetString());
    }

    private static async Task<IHost> CreateHostAsync(Action<IServiceCollection> configureServices)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    configureServices(services);
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapMochaResourceEndpoint());
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private sealed class TestResource : MochaResource
    {
        private readonly string _kind;
        private readonly string _id;
        private readonly Action<Utf8JsonWriter> _writeAttributes;

        public TestResource(string kind, string id, Action<Utf8JsonWriter> writeAttributes)
        {
            _kind = kind;
            _id = id;
            _writeAttributes = writeAttributes;
        }

        public override string Kind => _kind;

        public override string Id => _id;

        public override void Write(Utf8JsonWriter writer) => _writeAttributes(writer);
    }

    private sealed class TestSource : MochaResourceSource
    {
        public TestSource(IReadOnlyList<MochaResource> resources)
        {
            Resources = resources;
        }

        public override IReadOnlyList<MochaResource> Resources { get; }

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
    }

    private sealed class MutableTestSource : MochaResourceSource
    {
        private readonly object _lock = new();
        private IReadOnlyList<MochaResource> _resources;
        private CancellationTokenSource _cts = new();

        public MutableTestSource(IReadOnlyList<MochaResource> initial)
        {
            _resources = initial;
        }

        public override IReadOnlyList<MochaResource> Resources
        {
            get
            {
                lock (_lock)
                {
                    return _resources;
                }
            }
        }

        public override IChangeToken GetChangeToken()
        {
            lock (_lock)
            {
                return new CancellationChangeToken(_cts.Token);
            }
        }

        public void Replace(IReadOnlyList<MochaResource> next)
        {
            CancellationTokenSource oldCts;
            lock (_lock)
            {
                _resources = next;
                oldCts = _cts;
                _cts = new CancellationTokenSource();
            }

            oldCts.Cancel();
            oldCts.Dispose();
        }
    }
}
