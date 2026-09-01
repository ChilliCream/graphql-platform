using System.Collections.Immutable;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Sockets;
using HotChocolate.Fusion.Transport.Sockets.Client;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class WebSocketSourceSchemaClientTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_DialLazilyAndReuseSocket_When_ClientExecutesTwice()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var socket = new ScriptedWebSocket(SocketBehavior.Respond);
        var connectCount = 0;
        await using var client = CreateClient(invoker, socket, () => connectCount++);
        var context = fixture.CreateContext();
        var first = client.ExecuteAsync(
            context,
            CreateRequest(fixture.RootNode),
            TestContext.Current.CancellationToken);

        // act
        var countBeforeEnumeration = connectCount;
        var firstValue = await ReadValueAsync(first);
        var secondValue = await ReadValueAsync(
            client.ExecuteAsync(
                context,
                CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(0, countBeforeEnumeration);
        Assert.Equal(1, connectCount);
        Assert.Equal("value", firstValue);
        Assert.Equal("value", secondValue);
        Assert.Equal(2, socket.SubscribeCount);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseDifferentSocket_When_ClientBelongsToDifferentRequest()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var firstSocket = new ScriptedWebSocket(SocketBehavior.Respond);
        var secondSocket = new ScriptedWebSocket(SocketBehavior.Respond);
        var connectCount = 0;
        await using var firstClient = CreateClient(invoker, firstSocket, () => connectCount++);
        await using var secondClient = CreateClient(invoker, secondSocket, () => connectCount++);

        // act
        var firstValue = await ReadValueAsync(
            firstClient.ExecuteAsync(
                fixture.CreateContext(),
                CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));
        var secondValue = await ReadValueAsync(
            secondClient.ExecuteAsync(
                fixture.CreateContext(),
                CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(2, connectCount);
        Assert.Equal("value", firstValue);
        Assert.Equal("value", secondValue);
        Assert.Equal(1, firstSocket.SubscribeCount);
        Assert.Equal(1, secondSocket.SubscribeCount);
    }

    [Fact]
    public async Task DisposeAsync_Should_CompleteOperationAndCloseNormally_When_OperationIsActive()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var socket = new ScriptedWebSocket(SocketBehavior.Hold);
        var client = CreateClient(invoker, socket);
        var enumerator = client.SubscribeAsync(
                fixture.CreateContext(),
                CreateRequest(fixture.RootNode, OperationType.Subscription),
                TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var pendingResult = enumerator.MoveNextAsync().AsTask();
        await socket.Subscribed.Task.WaitAsync(TestContext.Current.CancellationToken);

        // act
        await client.DisposeAsync();
        var streamCompleted = !await pendingResult;
        await enumerator.DisposeAsync();

        // assert
        Assert.True(streamCompleted);
        Assert.Equal(1, socket.CompleteCount);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, socket.ClientCloseStatus);
        Assert.Equal(["complete", "close"], socket.Events);
    }

    [Fact]
    public async Task SubscribeAsync_Should_NotRedial_When_SocketDies()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var socket = new ScriptedWebSocket(SocketBehavior.Terminate);
        var connectCount = 0;
        await using var client = CreateClient(invoker, socket, () => connectCount++);
        var context = fixture.CreateContext();

        // act
        var firstError = await Assert.ThrowsAsync<SocketClosedException>(
            async () => await ReadValueAsync(
                client.SubscribeAsync(
                    context,
                    CreateRequest(fixture.RootNode, OperationType.Subscription),
                    TestContext.Current.CancellationToken)));
        var secondError = await Record.ExceptionAsync(
            async () => await ReadValueAsync(
                client.ExecuteAsync(
                    context,
                    CreateRequest(fixture.RootNode),
                    TestContext.Current.CancellationToken)));

        // assert
        Assert.Equal((WebSocketCloseStatus)1006, firstError.Reason);
        Assert.IsType<WebSocketException>(secondError);
        Assert.Equal(1, connectCount);
    }

    [Fact]
    public async Task DisposeAsync_Should_CompleteAndClose_When_ConnectionCreationIsPaused()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var socket = new ScriptedWebSocket(SocketBehavior.Hold);
        var connectionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowConnection = new TaskCompletionSource<WebSocket>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new WebSocketSourceSchemaClient(
            invoker,
            new WebSocketSourceSchemaClientConfiguration("A", new Uri("ws://localhost/graphql")),
            async (_, _, _, _) =>
            {
                connectionStarted.TrySetResult();
                return await allowConnection.Task;
            });
        var enumerator = client.ExecuteAsync(
                fixture.CreateContext(),
                CreateRequest(fixture.RootNode),
                TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var moveNext = enumerator.MoveNextAsync().AsTask();
        await connectionStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        // act
        var dispose = client.DisposeAsync().AsTask();
        allowConnection.TrySetResult(socket);
        await dispose;
        var streamCompleted = !await moveNext;
        await enumerator.DisposeAsync();

        // assert
        Assert.True(streamCompleted);
        Assert.Equal(["complete", "close"], socket.Events);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReleaseUnclaimedSharedResult_When_EnumerationStops()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var socket = new ScriptedWebSocket(SocketBehavior.Respond);
        await using var client = CreateClient(
            invoker,
            socket,
            capabilities: SourceSchemaClientCapabilities.VariableBatching);
        var enumerator = client.ExecuteAsync(
                fixture.CreateContext(),
                CreateRequest(fixture.RootNode, variableCount: 2),
                TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        Assert.True(await enumerator.MoveNextAsync());
        var data = enumerator.Current.Data;
        enumerator.Current.Dispose();
        await enumerator.DisposeAsync();

        // assert
        Assert.Throws<ObjectDisposedException>(() => data.GetProperty("field"));
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_ReleaseUnclaimedSharedResult_When_EnumerationStops()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var socket = new ScriptedWebSocket(SocketBehavior.Respond);
        await using var client = CreateClient(
            invoker,
            socket,
            capabilities: SourceSchemaClientCapabilities.VariableBatching);
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode, variableCount: 2));
        var enumerator = client.ExecuteBatchAsync(
                fixture.CreateContext(),
                requests,
                TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        Assert.True(await enumerator.MoveNextAsync());
        var data = enumerator.Current.Result.Data;
        enumerator.Current.Result.Dispose();
        await enumerator.DisposeAsync();

        // assert
        Assert.Throws<ObjectDisposedException>(() => data.GetProperty("field"));
    }

    [Fact]
    public async Task ExecuteAsync_Should_RejectInvalidVariableIndex_When_VariableBatchingIsEnabled()
    {
        // arrange
        await using var fixture = await WebSocketClientTestFixture.CreateAsync();
        using var invoker = new HttpMessageInvoker(new StubHttpMessageHandler());
        var socket = new ScriptedWebSocket(SocketBehavior.Hold);
        await using var client = CreateClient(
            invoker,
            socket,
            capabilities: SourceSchemaClientCapabilities.VariableBatching);
        var enumerator = client.ExecuteAsync(
                fixture.CreateContext(),
                CreateRequest(fixture.RootNode, variableCount: 2),
                TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var moveNext = enumerator.MoveNextAsync().AsTask();
        await socket.Subscribed.Task.WaitAsync(TestContext.Current.CancellationToken);
        socket.SendPayload("{\"data\":{\"field\":\"value\"},\"variableIndex\":2}");

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await moveNext);
        await enumerator.DisposeAsync();

        // assert
        Assert.Equal("The batch response contains an out-of-range variableIndex '2'.", exception.Message);
    }

    [Fact]
    public async Task CreateClient_Should_CreateAndDisposeOneInvoker_When_CalledConcurrently()
    {
        // arrange
        var created = 0;
        var handler = new TrackingHttpMessageHandler();
        using var factory = new WebSocketSourceSchemaClientFactory(
            () =>
            {
                Interlocked.Increment(ref created);
                return new HttpMessageInvoker(handler);
            });
        var configuration = new WebSocketSourceSchemaClientConfiguration(
            "A",
            new Uri("ws://localhost/graphql"));

        // act
        await Task.WhenAll(
            Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() => factory.CreateClient(null!, configuration))));
        factory.Dispose();

        // assert
        Assert.Equal(1, created);
        Assert.Equal(1, handler.DisposeCount);
    }

    private static WebSocketSourceSchemaClient CreateClient(
        HttpMessageInvoker invoker,
        ScriptedWebSocket socket,
        Action? onConnect = null,
        SourceSchemaClientCapabilities capabilities = SourceSchemaClientCapabilities.Default)
        => new(
            invoker,
            new WebSocketSourceSchemaClientConfiguration(
                "A",
                new Uri("ws://localhost/graphql"),
                capabilities: capabilities),
            (_, _, _, _) =>
            {
                onConnect?.Invoke();
                return ValueTask.FromResult<WebSocket>(socket);
            });

    private static SourceSchemaClientRequest CreateRequest(
        ExecutionNode node,
        OperationType operationType = OperationType.Query,
        int variableCount = 1)
    {
        var sourceText = operationType is OperationType.Subscription
            ? "subscription { field }"u8.ToArray()
            : "query { field }"u8.ToArray();

        return new SourceSchemaClientRequest
        {
            Node = node,
            SchemaName = "A",
            OperationType = operationType,
            OperationSourceText = new OperationSourceText(
                "Op",
                operationType,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            OperationDocument = Utf8GraphQLOperationParser.Parse(sourceText),
            Variables = Enumerable
                .Range(0, variableCount)
                .Select(_ => new VariableValues(CompactPath.Root, JsonSegment.Empty))
                .ToImmutableArray()
        };
    }

    private static async Task<string?> ReadValueAsync(IAsyncEnumerable<SourceSchemaResult> results)
    {
        await foreach (var result in results.WithCancellation(TestContext.Current.CancellationToken))
        {
            using (result)
            {
                return result.Data.GetProperty("field").GetString();
            }
        }

        return null;
    }

    private sealed class WebSocketClientTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlan _operationPlan;
        private readonly List<OperationPlanContext> _contexts = [];
        private readonly List<CancellationTokenSource> _cancellationTokenSources = [];
        private readonly List<(ObjectPool<PooledRequestContext> Pool, PooledRequestContext Context)>
            _requestContexts = [];

        private WebSocketClientTestFixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlan operationPlan)
        {
            _services = services;
            _executor = executor;
            _operationPlan = operationPlan;
        }

        public ExecutionNode RootNode => _operationPlan.RootNodes[0];

        public static async Task<WebSocketClientTestFixture> CreateAsync()
        {
            var services = new ServiceCollection()
                .AddHttpClient()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        """
                        type Query {
                          field: String!
                        }

                        type Subscription {
                          field: String!
                        }
                        """))
                .Services
                .BuildServiceProvider();
            var executor = await services.GetRequestExecutorAsync();
            var schema = (Fusion.Types.FusionSchemaDefinition)executor.Schema;
            var operationPlan = PlanOperation(schema, "query { field }");
            return new WebSocketClientTestFixture(services, executor, operationPlan);
        }

        public OperationPlanContext CreateContext()
        {
            var contextPool = _executor.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            var context = contextPool.Rent();
            var contextCts = new CancellationTokenSource();
            var requestContextPool =
                _executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
            var requestContext = requestContextPool.Get();
            var request = OperationRequestBuilder.New().SetDocument("{ field }").Build();

            requestContext.Initialize(
                _executor.Schema,
                _executor.Version,
                request,
                requestIndex: 0,
                requestServices: _services,
                requestAborted: CancellationToken.None);
            context.Initialize(
                requestContext,
                VariableValueCollection.Empty,
                _operationPlan,
                contextCts,
                new MemoryArena());

            _contexts.Add(context);
            _cancellationTokenSources.Add(contextCts);
            _requestContexts.Add((requestContextPool, requestContext));
            return context;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var context in _contexts)
            {
                await context.DisposeAsync();
            }

            foreach (var (pool, context) in _requestContexts)
            {
                pool.Return(context);
            }

            foreach (var cancellationTokenSource in _cancellationTokenSources)
            {
                cancellationTokenSource.Dispose();
            }

            await _services.DisposeAsync();
        }
    }

    private enum SocketBehavior
    {
        Respond,
        Hold,
        Terminate
    }

    private sealed class ScriptedWebSocket(SocketBehavior behavior) : WebSocket
    {
        private readonly Channel<byte[]?> _receivedMessages = Channel.CreateUnbounded<byte[]?>();
        private WebSocketState _state = WebSocketState.Open;
        private WebSocketCloseStatus? _closeStatus;
        private string? _closeStatusDescription;
        private int _completeCount;
        private int _subscribeCount;
        private string? _operationId;
        private readonly List<string> _events = [];

        public TaskCompletionSource Subscribed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WebSocketCloseStatus? ClientCloseStatus { get; private set; }

        public int CompleteCount => Volatile.Read(ref _completeCount);

        public int SubscribeCount => Volatile.Read(ref _subscribeCount);

        public IReadOnlyList<string> Events
        {
            get
            {
                lock (_events)
                {
                    return _events.ToArray();
                }
            }
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string? CloseStatusDescription => _closeStatusDescription;

        public override string SubProtocol => WellKnownProtocols.GraphQL_Transport_WS;

        public override WebSocketState State => _state;

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
            _receivedMessages.Writer.TryWrite(null);
        }

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            ClientCloseStatus = closeStatus;
            _closeStatus = closeStatus;
            _closeStatusDescription = statusDescription;
            _state = WebSocketState.Closed;
            lock (_events)
            {
                _events.Add("close");
            }
            _receivedMessages.Writer.TryWrite(null);
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => CloseAsync(closeStatus, statusDescription, cancellationToken);

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
            _receivedMessages.Writer.TryComplete();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            var result = await ReceiveAsync(buffer.AsMemory(), cancellationToken);
            return new WebSocketReceiveResult(
                result.Count,
                result.MessageType,
                result.EndOfMessage,
                _closeStatus,
                _closeStatusDescription);
        }

        public override async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var message = await _receivedMessages.Reader.ReadAsync(cancellationToken);

            if (message is null)
            {
                return new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            }

            message.CopyTo(buffer);
            return new ValueWebSocketReceiveResult(message.Length, WebSocketMessageType.Text, true);
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
            => SendAsync(buffer.AsMemory(), messageType, endOfMessage, cancellationToken).AsTask();

        public override ValueTask SendAsync(
            ReadOnlyMemory<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken = default)
        {
            if (_state is not WebSocketState.Open)
            {
                return ValueTask.FromException(new WebSocketException("The socket is closed."));
            }

            using var document = JsonDocument.Parse(buffer);
            var type = document.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "connection_init":
                    Enqueue("""{"type":"connection_ack"}""");
                    break;

                case "subscribe":
                    var id = document.RootElement.GetProperty("id").GetString()!;
                    _operationId = id;
                    Interlocked.Increment(ref _subscribeCount);
                    Subscribed.TrySetResult();

                    if (behavior is SocketBehavior.Respond)
                    {
                        Enqueue(
                            string.Concat(
                                "{\"type\":\"next\",\"id\":",
                                JsonSerializer.Serialize(id),
                                ",\"payload\":{\"data\":{\"field\":\"value\"}}}"));
                        Enqueue(
                            string.Concat(
                                "{\"type\":\"complete\",\"id\":",
                                JsonSerializer.Serialize(id),
                                "}"));
                    }
                    else if (behavior is SocketBehavior.Terminate)
                    {
                        _state = WebSocketState.Aborted;
                        _receivedMessages.Writer.TryWrite(null);
                    }
                    break;

                case "complete":
                    Interlocked.Increment(ref _completeCount);
                    lock (_events)
                    {
                        _events.Add("complete");
                    }
                    break;
            }

            return ValueTask.CompletedTask;
        }

        public void SendPayload(string payload)
            => Enqueue(string.Concat(
                "{\"type\":\"next\",\"id\":",
                JsonSerializer.Serialize(_operationId),
                ",\"payload\":",
                payload,
                "}"));

        private void Enqueue(string message)
            => _receivedMessages.Writer.TryWrite(Encoding.UTF8.GetBytes(message));
    }

    private sealed class TrackingHttpMessageHandler : HttpMessageHandler
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Increment(ref _disposeCount);
            }

            base.Dispose(disposing);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
    }
}
