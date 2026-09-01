using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Transport.Sockets;
using HotChocolate.Transport.Sockets.Client;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using FusionGraphQLHttpClient = HotChocolate.Fusion.Transport.Http.GraphQLHttpClient;
using OperationRequest = HotChocolate.Transport.OperationRequest;
using TransportOperationResult = HotChocolate.Transport.OperationResult;

namespace HotChocolate.Fusion;

[Collection("WebSocketTransportTests")]
public sealed class WebSocketTransportTests : FusionTestBase
{
    [Fact]
    public async Task WebSocketsOnly_Should_RouteQueryMutationAndSubscription_OverWebSocket()
    {
        // arrange
        var connections = new ConnectionCapture();
        using var source = CreateSourceSchema(
            "A",
            builder => builder
                .AddQueryType<SourceSchema.Query>()
                .AddMutationType<SourceSchema.Mutation>()
                .AddSubscriptionType<SourceSchema.Subscription>()
                .AddSocketSessionInterceptor(_ => new ConnectionCaptureInterceptor(connections)));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", source)],
            configureServices: services => ReplaceWebSocketClientFactory(services, [("A", source)]),
            gatewaySettings: WebSocketOnlySettings);

        // act
        var query = await ExecuteFiniteOverWebSocketAsync(gateway, "{ queryValue }");
        var mutation = await ExecuteFiniteOverWebSocketAsync(gateway, "mutation { setValue }");
        var subscription = await ExecuteSubscriptionOverWebSocketAsync(
            gateway,
            "subscription { onMessage }");

        // assert
        new[] { query.GetRawText(), mutation.GetRawText(), subscription.GetRawText() }
            .MatchInlineSnapshots(
                [
                    """{"queryValue":"query"}""",
                    """{"setValue":"mutation"}""",
                    """{"onMessage":"subscription"}"""
                ]);
        Assert.Equal(3, connections.Count);
    }

    [Fact]
    public async Task HttpAndWebSockets_Should_RouteQueryOverHttpAndSubscriptionOverWebSocket()
    {
        // arrange
        var connections = new ConnectionCapture();
        using var source = CreateSourceSchema(
            "A",
            builder => builder
                .AddQueryType<SourceSchema.Query>()
                .AddSubscriptionType<SourceSchema.Subscription>()
                .AddSocketSessionInterceptor(_ => new ConnectionCaptureInterceptor(connections)));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", source)],
            configureServices: services => ReplaceWebSocketClientFactory(services, [("A", source)]),
            gatewaySettings: HttpAndWebSocketSettings);

        // act
        var query = await ExecuteOverHttpAsync(gateway, "{ queryValue }");
        var subscription = await ExecuteSubscriptionOverWebSocketAsync(
            gateway,
            "subscription { onMessage }");

        // assert
        new[] { query.GetRawText(), subscription.GetRawText() }.MatchInlineSnapshots(
            [
                """{"queryValue":"query"}""",
                """{"onMessage":"subscription"}"""
            ]);
        Assert.Equal(1, connections.Count);
    }

    [Fact]
    public async Task ContextForwarding_Should_ForwardConfiguredInputs_AndKeepDefaultsEmpty()
    {
        // arrange
        var configuredConnections = new ConnectionCapture();
        using var configuredSource = CreateSourceSchema(
            "A",
            builder => builder
                .AddQueryType<SourceSchema.Query>()
                .AddSocketSessionInterceptor(_ => new ConnectionCaptureInterceptor(configuredConnections)));
        using var configuredGateway = await CreateCompositeSchemaAsync(
            [("A", configuredSource)],
            configureServices: services => ReplaceWebSocketClientFactory(services, [("A", configuredSource)]),
            configureGatewayBuilder: builder => builder.AddWebSocketContextForwarding(
                context => context
                    .ForwardHttpHeader("x-token", WebSocketContextForwardingTargetKind.InitParameter, "header")
                    .ForwardClientInitProperty("client", WebSocketContextForwardingTargetKind.InitParameter, "init")),
            gatewaySettings: WebSocketOnlySettings);
        var defaultConnections = new ConnectionCapture();
        using var defaultSource = CreateSourceSchema(
            "A",
            builder => builder
                .AddQueryType<SourceSchema.Query>()
                .AddSocketSessionInterceptor(_ => new ConnectionCaptureInterceptor(defaultConnections)));
        using var defaultGateway = await CreateCompositeSchemaAsync(
            [("A", defaultSource)],
            configureServices: services => ReplaceWebSocketClientFactory(services, [("A", defaultSource)]),
            gatewaySettings: WebSocketOnlySettings);

        // act
        await ExecuteOverHttpAsync(configuredGateway, "{ queryValue }", "x-token", "header-value");
        await ExecuteFiniteOverWebSocketAsync(
            configuredGateway,
            "{ queryValue }",
            JsonSerializer.SerializeToElement(new { client = "init-value" }));
        await ExecuteFiniteOverWebSocketAsync(defaultGateway, "{ queryValue }");

        // assert
        configuredConnections.Payloads.Select(static p => p.GetRawText()).MatchInlineSnapshots(
            ["""{"header":"header-value"}""", """{"init":"init-value"}"""]);
        Assert.Equal(JsonValueKind.Undefined, Assert.Single(defaultConnections.Payloads).ValueKind);
    }

    [Fact]
    public async Task Subscription_Should_UseOneSocketPerSourceSchema_When_EntityLookupRuns()
    {
        // arrange
        var subscriptionConnections = new ConnectionCapture();
        var lookupConnections = new ConnectionCapture();
        var events = new EntityLookupEvents();
        using var subscriptions = CreateSourceSchema(
            "A",
            builder => builder
                .AddQueryType<EntityLookupSourceSchema1.Query>()
                .AddSubscriptionType<EntityLookupSourceSchema1.Subscription>()
                .AddSocketSessionInterceptor(_ => new ConnectionCaptureInterceptor(subscriptionConnections)),
            configureServices: services => services.AddSingleton(events));
        using var lookups = CreateSourceSchema(
            "B",
            builder => builder
                .AddQueryType<EntityLookupSourceSchema2.Query>()
                .AddSocketSessionInterceptor(_ => new ConnectionCaptureInterceptor(lookupConnections)));
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", subscriptions), ("B", lookups)],
            configureServices: services => ReplaceWebSocketClientFactory(
                services,
                [("A", subscriptions), ("B", lookups)]),
            gatewaySettings: TwoWebSocketSourcesSettings);

        var webSocketClient = gateway.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
            request.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;
        using var webSocket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        await using var client = await SocketClient.ConnectAsync(
            webSocket,
            TestContext.Current.CancellationToken);
        using var result = await client.ExecuteAsync(
            new OperationRequest("subscription { onBookCreated { id title } }"),
            TestContext.Current.CancellationToken);
        await using var results = result.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        events.ReleaseFirst();
        var first = await ReadNextResultAsync(results);
        events.ReleaseSecond();
        var second = await ReadNextResultAsync(results);
        events.Complete();
        var completed = !await results.MoveNextAsync().AsTask().WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // assert
        new[] { first.GetRawText(), second.GetRawText() }.MatchInlineSnapshots(
            [
                """{"onBookCreated":{"id":1,"title":"Foo"}}""",
                """{"onBookCreated":{"id":2,"title":"Bar"}}"""
            ]);
        Assert.True(completed);
        Assert.Equal(1, subscriptionConnections.Count);
        Assert.Equal(1, lookupConnections.Count);
    }

    [Fact]
    public async Task Subscription_Should_ReportTerminalErrorWithoutRedial_When_SourceSocketDies()
    {
        // arrange
        var connections = new ConnectionCapture();
        var exceptions = new SocketClosedExceptionCapture();
        using var source = CreateSourceSchema(
            "A",
            builder => builder
                .AddQueryType<SourceSchema.Query>()
                .AddSubscriptionType<SourceDeathSchema.Subscription>()
                .AddSocketSessionInterceptor(_ => new ClosingConnectionInterceptor(connections)));
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", source)],
            configureServices: services => ReplaceWebSocketClientFactory(services, [("A", source)]),
            configureGatewayBuilder: builder => builder.AddErrorFilter(error =>
            {
                exceptions.TryCapture(error.Exception);
                return error;
            }),
            gatewaySettings: WebSocketOnlySettings);

        // act
        var results = await ReadSubscriptionResultsAsync(
            gateway,
            "subscription { onSlowMessage }");

        // assert
        results.Select(static result => result.GetRawText()).MatchInlineSnapshots(
            [
                """{"data":{"onSlowMessage":"first"}}""",
                """{"data":null,"errors":[{"message":"Unexpected Execution Error","path":["onSlowMessage"]}]}"""
            ]);
        var exception = Assert.Single(exceptions.Items);
        Assert.Equal(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError, exception.Reason);
        Assert.Equal("test source socket death", exception.Message);
        Assert.Equal(1, connections.Count);
    }

    private const string WebSocketOnlySettings = """
        {
          "sourceSchemas": {
            "A": {
              "transports": {
                "websockets": {
                  "url": "ws://localhost:5000/graphql"
                }
              }
            }
          }
        }
        """;

    private const string HttpAndWebSocketSettings = """
        {
          "sourceSchemas": {
            "A": {
              "transports": {
                "http": {
                  "url": "http://localhost:5000/graphql"
                },
                "websockets": {
                  "url": "ws://localhost:5000/graphql"
                }
              }
            }
          }
        }
        """;

    private const string TwoWebSocketSourcesSettings = """
        {
          "sourceSchemas": {
            "A": {
              "transports": {
                "websockets": {
                  "url": "ws://localhost:5000/graphql"
                }
              }
            },
            "B": {
              "transports": {
                "websockets": {
                  "url": "ws://localhost:5000/graphql"
                }
              }
            }
          }
        }
        """;

    private static void ReplaceWebSocketClientFactory(
        IServiceCollection services,
        (string Name, TestServer Server)[] servers)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(ISourceSchemaClientFactory))
            {
                services.RemoveAt(i);
            }
        }

        services.AddSingleton<ISourceSchemaClientFactory>(new TestServerHttpSourceSchemaClientFactory(servers));
        services.AddSingleton<ISourceSchemaClientFactory>(new TestServerWebSocketSourceSchemaClientFactory(servers));
    }

    private static async Task<JsonElement> ExecuteOverHttpAsync(
        Gateway gateway,
        string document,
        string? headerName = null,
        string? headerValue = null)
    {
        using var client = gateway.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/graphql")
        {
            Content = JsonContent.Create(new { query = document })
        };

        if (headerName is not null && headerValue is not null)
        {
            request.Headers.Add(headerName, headerValue);
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        using var result = await new GraphQLHttpResponse(response).ReadAsResultAsync(TestContext.Current.CancellationToken);

        if (result.Data.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException(result.Errors.GetRawText());
        }

        return result.Data.Clone();
    }

    private static async Task<JsonElement> ExecuteFiniteOverWebSocketAsync(
        Gateway gateway,
        string document,
        JsonElement initPayload = default)
    {
        var webSocketClient = gateway.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
            request.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;

        using var webSocket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        await using var client = await SocketClient.ConnectAsync(
            webSocket,
            initPayload,
            TestContext.Current.CancellationToken);
        using var result = await client.ExecuteAsync(
            new OperationRequest(document),
            TestContext.Current.CancellationToken);

        JsonElement data = default;

        await foreach (var operationResult in result.ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            using (operationResult)
            {
                if (operationResult.Data.ValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException(operationResult.Errors.GetRawText());
                }

                if (data.ValueKind is not JsonValueKind.Undefined)
                {
                    throw new InvalidOperationException("The gateway operation produced more than one result.");
                }

                data = operationResult.Data.Clone();
            }
        }

        if (data.ValueKind is JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("The gateway operation did not produce a result.");
        }

        return data;
    }

    private static async Task<JsonElement> ReadNextResultAsync(IAsyncEnumerator<TransportOperationResult> results)
    {
        if (!await results.MoveNextAsync().AsTask().WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken))
        {
            throw new InvalidOperationException("The gateway subscription completed before producing a result.");
        }

        using var result = results.Current;

        if (result.Data.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException(result.Errors.GetRawText());
        }

        return result.Data.Clone();
    }

    private static async Task<JsonElement> ExecuteSubscriptionOverWebSocketAsync(
        Gateway gateway,
        string document)
    {
        var webSocketClient = gateway.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
            request.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;
        using var webSocket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        await using var client = await SocketClient.ConnectAsync(
            webSocket,
            TestContext.Current.CancellationToken);
        using var result = await client.ExecuteAsync(
            new OperationRequest(document),
            TestContext.Current.CancellationToken);
        await using var results = result.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);

        if (!await results.MoveNextAsync().AsTask().WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken))
        {
            throw new InvalidOperationException("The gateway subscription completed before producing a result.");
        }

        using var operationResult = results.Current;

        if (operationResult.Data.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException(operationResult.Errors.GetRawText());
        }

        return operationResult.Data.Clone();
    }

    private static async Task<IReadOnlyList<JsonElement>> ReadSubscriptionResultsAsync(
        Gateway gateway,
        string document)
    {
        var webSocketClient = gateway.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
            request.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;

        using var webSocket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        await using var client = await SocketClient.ConnectAsync(webSocket, TestContext.Current.CancellationToken);
        using var result = await client.ExecuteAsync(
            new OperationRequest(document),
            TestContext.Current.CancellationToken);
        var results = new List<JsonElement>();

        await foreach (var operationResult in result.ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            using (operationResult)
            {
                results.Add(CreateResponsePayload(operationResult));
            }
        }

        return results;
    }

    private static JsonElement CreateResponsePayload(TransportOperationResult result)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();

        if (result.Data.ValueKind is not JsonValueKind.Undefined)
        {
            writer.WritePropertyName("data");
            result.Data.WriteTo(writer);
        }

        if (result.Errors.ValueKind is not JsonValueKind.Undefined)
        {
            writer.WritePropertyName("errors");
            result.Errors.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        using var document = JsonDocument.Parse(buffer.WrittenMemory);
        return document.RootElement.Clone();
    }

    private sealed class TestServerWebSocketSourceSchemaClientFactory(
        (string Name, TestServer Server)[] servers)
        : SourceSchemaClientFactory<WebSocketSourceSchemaClientConfiguration>, IDisposable
    {
        private readonly Dictionary<string, TestServer> _servers = servers.ToDictionary(static t => t.Name, static t => t.Server);
        private readonly HttpMessageInvoker _invoker = new(new HttpClientHandler());

        protected override ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            WebSocketSourceSchemaClientConfiguration configuration)
        {
            var server = _servers[configuration.Name];
            return new WebSocketSourceSchemaClient(
                _invoker,
                configuration,
                async (url, _, _, headers, cancellationToken) =>
                {
                    var client = server.CreateWebSocketClient();
                    client.ConfigureRequest = request =>
                    {
                        request.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;

                        foreach (var (name, value) in headers)
                        {
                            request.Headers[name] = value;
                        }
                    };
                    var socket = await client.ConnectAsync(url, cancellationToken);
                    return new TestServerWebSocket(socket);
                });
        }

        public void Dispose() => _invoker.Dispose();
    }

    private sealed class TestServerHttpSourceSchemaClientFactory(
        (string Name, TestServer Server)[] servers)
        : SourceSchemaClientFactory<HttpSourceSchemaClientConfiguration>
    {
        private readonly Dictionary<string, TestServer> _servers = servers.ToDictionary(static t => t.Name, static t => t.Server);

        protected override ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            HttpSourceSchemaClientConfiguration configuration)
        {
            var client = new HttpClient(_servers[configuration.Name].CreateHandler())
            {
                BaseAddress = _servers[configuration.Name].BaseAddress
            };
            return new HttpSourceSchemaClient(
                FusionGraphQLHttpClient.Create(client, disposeHttpClient: true),
                configuration);
        }
    }

    private sealed class ConnectionCapture
    {
        public ConcurrentQueue<JsonElement> Payloads { get; } = [];

        public int Count => Payloads.Count;
    }

    private sealed class SocketClosedExceptionCapture
    {
        public ConcurrentQueue<HotChocolate.Fusion.Transport.Sockets.Client.SocketClosedException> Items { get; } = [];

        public void TryCapture(Exception? exception)
        {
            if (exception is HotChocolate.Fusion.Transport.Sockets.Client.SocketClosedException socketClosedException)
            {
                Items.Enqueue(socketClosedException);
            }
        }
    }

    private sealed class EntityLookupEvents
    {
        private readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseFirst() => _first.SetResult();

        public void ReleaseSecond() => _second.SetResult();

        public void Complete() => _completion.SetResult();

        public async IAsyncEnumerable<EntityLookupSourceSchema1.Book> ReadAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await _first.Task.WaitAsync(cancellationToken);
            yield return new EntityLookupSourceSchema1.Book(1);

            await _second.Task.WaitAsync(cancellationToken);
            yield return new EntityLookupSourceSchema1.Book(2);

            await _completion.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class ClosingConnectionInterceptor(ConnectionCapture capture) : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            capture.Payloads.Enqueue(connectionInitMessage.Payload?.Clone() ?? default);
            return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
        }

        public override ValueTask OnRequestAsync(
            ISocketSession session,
            string operationSessionId,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken = default)
        {
            _ = CloseAsync(session.Connection);
            return base.OnRequestAsync(session, operationSessionId, requestBuilder, cancellationToken);
        }

        private static async Task CloseAsync(ISocketConnection connection)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await connection.CloseAsync("test source socket death", ConnectionCloseReason.InternalServerError);
        }
    }

    private sealed class TestServerWebSocket(WebSocket inner) : WebSocket
    {
        public override WebSocketCloseStatus? CloseStatus => inner.CloseStatus;

        public override string? CloseStatusDescription => inner.CloseStatusDescription;

        public override string? SubProtocol => inner.SubProtocol;

        public override WebSocketState State => inner.State;

        public override void Abort() => inner.Abort();

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => CloseCoreAsync(closeStatus, statusDescription, cancellationToken).AsTask();

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => inner.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

        public override void Dispose() => inner.Dispose();

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
            => inner.ReceiveAsync(buffer, cancellationToken);

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken)
            => inner.ReceiveAsync(buffer, cancellationToken);

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
            => inner.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        public override ValueTask SendAsync(
            ReadOnlyMemory<byte> buffer,
            WebSocketMessageType messageType,
            WebSocketMessageFlags endOfMessage,
            CancellationToken cancellationToken)
            => inner.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        private async ValueTask CloseCoreAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            try
            {
                await inner.CloseAsync(closeStatus, statusDescription, cancellationToken);
            }
            catch (Exception exception) when (
                exception is ObjectDisposedException
                    or WebSocketException
                    or IOException { InnerException: ObjectDisposedException })
            {
            }
        }
    }

    private sealed class ConnectionCaptureInterceptor(ConnectionCapture capture) : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            capture.Payloads.Enqueue(connectionInitMessage.Payload?.Clone() ?? default);
            return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
        }
    }

    private static class SourceSchema
    {
        public sealed class Query
        {
            public string QueryValue() => "query";
        }

        public sealed class Mutation
        {
            public string SetValue() => "mutation";
        }

        public sealed class Subscription
        {
            public async IAsyncEnumerable<string> OnMessageStream(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return "subscription";
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            [Subscribe(With = nameof(OnMessageStream))]
            public string OnMessage([EventMessage] string message) => message;
        }
    }

    private static class EntityLookupSourceSchema1
    {
        [EntityKey("id")]
        public sealed record Book(int Id);

        public sealed class Query
        {
            public string Value() => "value";
        }

        public sealed class Subscription(EntityLookupEvents events)
        {
            public IAsyncEnumerable<Book> OnBookCreatedStream(CancellationToken cancellationToken)
                => events.ReadAsync(cancellationToken);

            [Subscribe(With = nameof(OnBookCreatedStream))]
            public Book OnBookCreated([EventMessage] Book book) => book;
        }
    }

    private static class EntityLookupSourceSchema2
    {
        public sealed record Book(int Id, string Title);

        public sealed class Query
        {
            [Internal, Lookup]
            public Book? GetBookById(int id)
                => id switch
                {
                    1 => new Book(1, "Foo"),
                    2 => new Book(2, "Bar"),
                    _ => null
                };
        }
    }

    private static class SourceDeathSchema
    {
        [GraphQLName("Subscription")]
        public sealed class Subscription
        {
            public async IAsyncEnumerable<string> OnSlowMessageStream()
            {
                yield return "first";
                await Task.Delay(TimeSpan.FromSeconds(1));
                yield return "second";
            }

            [Subscribe(With = nameof(OnSlowMessageStream))]
            public string OnSlowMessage([EventMessage] string message) => message;
        }
    }
}

[CollectionDefinition("WebSocketTransportTests", DisableParallelization = true)]
public sealed class WebSocketTransportTestCollection;
