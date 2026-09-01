using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class WebSocketContextForwardingTests : FusionTestBase
{
    [Fact]
    public async Task Forwarding_Should_ForwardHeadersAndClientInitProperties_ToAllTargets()
    {
        // arrange
        await using var fixture = await WebSocketSourceSchemaClientTests.WebSocketClientTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["x-header"] = "header-value";
        context.Features.Set<HttpContext>(httpContext);
        SetClientInitPayload(context, """{"clientProperty":"client-value"}""");
        var forwarding = new WebSocketContextForwardingBuilder()
            .ForwardHttpHeader("x-header", WebSocketContextForwardingTargetKind.InitParameter, "headerParam")
            .ForwardHttpHeader("x-header", WebSocketContextForwardingTargetKind.UpgradeHeader, "x-upgrade")
            .ForwardClientInitProperty(
                "clientProperty",
                WebSocketContextForwardingTargetKind.InitParameter,
                "clientParam")
            .ForwardClientInitProperty(
                "clientProperty",
                WebSocketContextForwardingTargetKind.UpgradeHeader,
                "x-client-upgrade")
            .Build();
        var (socket, headers, client) = CreateClient(forwarding);

        // act
        await WebSocketSourceSchemaClientTests.ReadValueAsync(
            client.ExecuteAsync(
                context,
                WebSocketSourceSchemaClientTests.CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));
        await client.DisposeAsync();

        // assert
        socket.ConnectionInitPayload.GetRawText().MatchInlineSnapshot(
            """{"headerParam":"header-value","clientParam":"client-value"}""");
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["x-upgrade"] = "header-value",
                ["x-client-upgrade"] = "client-value"
            },
            headers);
    }

    [Fact]
    public async Task Forwarding_Should_SkipMissingInputs_AndKeepDefaultConnectionInitEmpty()
    {
        // arrange
        await using var fixture = await WebSocketSourceSchemaClientTests.WebSocketClientTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        var forwarding = new WebSocketContextForwardingBuilder()
            .ForwardHttpHeader("x-missing", WebSocketContextForwardingTargetKind.InitParameter)
            .ForwardClientInitProperty("missing", WebSocketContextForwardingTargetKind.UpgradeHeader)
            .Build();
        var (socket, headers, client) = CreateClient(forwarding);

        // act
        await WebSocketSourceSchemaClientTests.ReadValueAsync(
            client.ExecuteAsync(
                context,
                WebSocketSourceSchemaClientTests.CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));
        await client.DisposeAsync();

        // assert
        Assert.Equal(JsonValueKind.Undefined, socket.ConnectionInitPayload.ValueKind);
        Assert.Empty(headers);
    }

    [Fact]
    public async Task Forwarding_Should_AllowHookToOverrideRules_OncePerConnection()
    {
        // arrange
        await using var fixture = await WebSocketSourceSchemaClientTests.WebSocketClientTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["x-token"] = "forwarded";
        context.Features.Set<HttpContext>(httpContext);
        var invoked = 0;
        var forwarding = new WebSocketContextForwardingBuilder()
            .ForwardHttpHeader("x-token", WebSocketContextForwardingTargetKind.InitParameter, "token")
            .ForwardHttpHeader("x-token", WebSocketContextForwardingTargetKind.UpgradeHeader, "x-token")
            .ConfigureConnection(
                (_, payload, headers) =>
                {
                    invoked++;
                    payload["token"] = "overridden";
                    headers["x-token"] = "overridden";
                })
            .Build();
        var (socket, headers, client) = CreateClient(forwarding);

        // act
        await WebSocketSourceSchemaClientTests.ReadValueAsync(
            client.ExecuteAsync(
                context,
                WebSocketSourceSchemaClientTests.CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));
        await WebSocketSourceSchemaClientTests.ReadValueAsync(
            client.ExecuteAsync(
                context,
                WebSocketSourceSchemaClientTests.CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));
        await client.DisposeAsync();

        // assert
        Assert.Equal(1, invoked);
        socket.ConnectionInitPayload.GetRawText().MatchInlineSnapshot("""{"token":"overridden"}""");
        Assert.Equal("overridden", headers["x-token"]);
    }

    [Fact]
    public void Builder_Should_KeepRulesAndExcludeGlobal_When_ConfiguredForSourceSchema()
    {
        // arrange
        var builder = new WebSocketContextForwardingBuilder()
            .ForwardHttpHeader("x-token", WebSocketContextForwardingTargetKind.InitParameter, "token")
            .ExcludeGlobal();

        // act
        var configuration = builder.Build();

        // assert
        Assert.True(configuration.ExcludeGlobalRules);
        Assert.Collection(
            configuration.Rules,
            rule => Assert.Equal("token", rule.TargetName));
    }

    [Fact]
    public async Task Forwarding_Should_ApplyGlobalRulesUnlessSourceSchemaExcludesThem()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(ComposeSchemaDocument("""
                type Query {
                  field: String!
                }
                """))
            .AddWebSocketClientConfiguration("A", new Uri("ws://localhost/graphql"))
            .AddWebSocketContextForwarding(
                builder => builder.ForwardHttpHeader(
                    "x-global",
                    WebSocketContextForwardingTargetKind.InitParameter))
            .AddWebSocketContextForwarding(
                "A",
                builder => builder.ForwardHttpHeader(
                    "x-source",
                    WebSocketContextForwardingTargetKind.InitParameter))
            .Services
            .BuildServiceProvider();
        await using var serviceProvider = services;
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var configurations = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();

        // act
        configurations.TryGet("A", OperationType.Query, out var configuredClient);
        var configuration = Assert.IsType<WebSocketSourceSchemaClientConfiguration>(configuredClient);

        // assert
        Assert.Collection(
            configuration.ContextForwarding!.Rules,
            rule => Assert.Equal("x-global", rule.SourceName),
            rule => Assert.Equal("x-source", rule.SourceName));
    }

    [Fact]
    public async Task Forwarding_Should_ExcludeGlobalRules_When_SourceSchemaOptsOut()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(ComposeSchemaDocument("""
                type Query {
                  field: String!
                }
                """))
            .AddWebSocketClientConfiguration("A", new Uri("ws://localhost/graphql"))
            .AddWebSocketContextForwarding(
                builder => builder.ForwardHttpHeader(
                    "x-global",
                    WebSocketContextForwardingTargetKind.InitParameter))
            .AddWebSocketContextForwarding(
                "A",
                builder => builder
                    .ExcludeGlobal()
                    .ForwardHttpHeader("x-source", WebSocketContextForwardingTargetKind.InitParameter))
            .Services
            .BuildServiceProvider();
        await using var serviceProvider = services;
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var configurations = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();

        // act
        configurations.TryGet("A", OperationType.Query, out var configuredClient);
        var configuration = Assert.IsType<WebSocketSourceSchemaClientConfiguration>(configuredClient);

        // assert
        Assert.Collection(
            configuration.ContextForwarding!.Rules,
            rule => Assert.Equal("x-source", rule.SourceName));
    }

    [Fact]
    public async Task Forwarding_Should_SendNoPayloadOrHeaders_When_NotConfigured()
    {
        // arrange
        await using var fixture = await WebSocketSourceSchemaClientTests.WebSocketClientTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        var (socket, headers, client) = CreateClient(forwarding: null);

        // act
        await WebSocketSourceSchemaClientTests.ReadValueAsync(
            client.ExecuteAsync(
                context,
                WebSocketSourceSchemaClientTests.CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));
        await client.DisposeAsync();

        // assert
        Assert.Equal(JsonValueKind.Undefined, socket.ConnectionInitPayload.ValueKind);
        Assert.Empty(headers);
    }

    private static (
        WebSocketSourceSchemaClientTests.ScriptedWebSocket Socket,
        Dictionary<string, string> Headers,
        WebSocketSourceSchemaClient Client) CreateClient(
        WebSocketContextForwardingConfiguration? forwarding)
    {
        var socket = new WebSocketSourceSchemaClientTests.ScriptedWebSocket(
            WebSocketSourceSchemaClientTests.SocketBehavior.Respond);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var configuration = new WebSocketSourceSchemaClientConfiguration(
            "A",
            new Uri("ws://localhost/graphql"))
        {
            ContextForwarding = forwarding
        };
        var client = new WebSocketSourceSchemaClient(
            new HttpMessageInvoker(new StubHttpMessageHandler()),
            configuration,
            (_, _, _, upgradeHeaders, _) =>
            {
                foreach (var (name, value) in upgradeHeaders)
                {
                    headers[name] = value;
                }

                return ValueTask.FromResult<WebSocket>(socket);
            });
        return (socket, headers, client);
    }

    private static void SetClientInitPayload(OperationPlanContext context, string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var connection = new TestConnection();
        connection.Features.Set(document.RootElement.Clone());
        context.RequestContext.ContextData["ISocketSession"] = new TestSocketSession(connection);
    }

    private sealed class TestSocketSession(TestConnection connection)
    {
        public TestConnection Connection { get; } = connection;
    }

    private sealed class TestConnection : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
    }
}
