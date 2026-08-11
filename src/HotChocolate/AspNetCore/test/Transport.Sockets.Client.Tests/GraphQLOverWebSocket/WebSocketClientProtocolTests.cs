using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.Tests;
using HotChocolate.Transport;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Transport.Sockets.GraphQLOverWebSocket;

public class WebSocketClientProtocolTests(TestServerFactory serverFactory, ITestOutputHelper output)
    : SubscriptionTestBase(serverFactory)
{
    [Fact]
    public Task Send_Connect_Accept()
        => SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    // arrange
                    using var testServer = CreateStarWarsServer(output: output);
                    var webSocketClient = CreateWebSocketClient(testServer);
                    using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

                    // act
                    await SocketClient.ConnectAsync(webSocket, ct);

                    // assert
                    // no error
                })
            .RunAsync();

    [Fact]
    public Task Subscribe_ReceiveDataOnMutation()
        => SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    // arrange
                    var subscriptionRequest = new OperationRequest(
                        "subscription { onReview(episode: NEW_HOPE) { stars } }");

                    var mutationRequest = new ClientQueryRequest
                    {
                        Query =
                            """
                            mutation {
                                createReview(episode: NEW_HOPE review: {
                                    commentary: "foo"
                                    stars: 5
                                }) {
                                    stars
                                }
                            }
                            """
                    };

                    using var testServer = CreateStarWarsServer(output: output);
                    var webSocketClient = CreateWebSocketClient(testServer);
                    using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

                    var client = await SocketClient.ConnectAsync(webSocket, ct);
                    string? result = null;

                    // act
                    // ... subscribe
                    using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

                    // ... trigger event
                    await testServer.SendPostRequestAsync(mutationRequest);

                    // receive event result on the stream
                    await foreach (var operationResult in
                        socketResult.ReadResultsAsync().WithCancellation(ct))
                    {
                        result = operationResult.Data.ToString();
                        operationResult.Dispose();
                        break;
                    }

                    // assert
                    snapshot.Add(result);
                })
            .MatchAsync();

    [Fact]
    public Task Subscribe_Should_Throw_When_Socket_Aborted_Without_Close_Frame()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            using var testServer = CreateStarWarsServer(output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            // ... subscribe
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // ... abort the client socket without a close frame, simulating abnormal loss
            webSocket.Abort();

            // assert
            async Task ReadResults()
            {
                await foreach (var result in socketResult.ReadResultsAsync().WithCancellation(ct))
                {
                    result.Dispose();
                }
            }

            var error = await Assert.ThrowsAsync<SocketClosedException>(ReadResults);
            Assert.Equal((WebSocketCloseStatus)1006, error.Reason);
        });
    }

    [Fact]
    public Task ReadResultsAsync_Should_Complete_When_Execute_Token_Is_Canceled()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            // server acks the connection, accepts the subscription and sends a single next
            // frame, then holds the socket open without closing or sending a complete.
            using var testServer = CreateSubscriptionMessageServer(
                id => $$$$"""{"id":"{{{{id}}}}","payload":{"data":{"event":{"type":"ping"}}},"type":"next"}""");
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var executeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, executeCts.Token);

            // act
            // enumerate without WithCancellation so the enumerator uses CancellationToken.None
            var enumeration = Task.Run(
                async () =>
                {
                    await foreach (var result in socketResult.ReadResultsAsync())
                    {
                        result.Dispose();
                    }
                },
                ct);

            executeCts.Cancel();

            // assert
            var finished = await Task.WhenAny(enumeration, Task.Delay(TimeSpan.FromSeconds(5), ct));
            Assert.Same(enumeration, finished);
            await enumeration;
        });
    }

    [Fact]
    public Task ReadResultsAsync_Should_Throw_SocketClosedException_When_Server_Closes_Connection()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            using var testServer = CreateClosingSubscriptionServer(nextMessageCount: 0);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // act
            async Task ReadResults()
            {
                await foreach (var result in socketResult.ReadResultsAsync().WithCancellation(ct))
                {
                    result.Dispose();
                }
            }

            // assert
            var error = await Assert.ThrowsAsync<SocketClosedException>(ReadResults);
            Assert.Equal((WebSocketCloseStatus)1012, error.Reason);
        });
    }

    [Fact]
    public Task ReadResultsAsync_Should_Throw_SocketClosedException_When_Server_Closes_Connection_After_Next_Messages()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            var received = new List<int>();

            using var testServer = CreateClosingSubscriptionServer(nextMessageCount: 5);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // act
            async Task ReadResults()
            {
                await foreach (var result in socketResult.ReadResultsAsync().WithCancellation(ct))
                {
                    received.Add(result.Data.GetProperty("value").GetInt32());
                    result.Dispose();
                }
            }

            // assert
            var error = await Assert.ThrowsAsync<SocketClosedException>(ReadResults);
            Assert.Equal(5, received.Count);
            Assert.Equal((WebSocketCloseStatus)1012, error.Reason);
        });
    }

    [Fact]
    public Task ReadResultsAsync_Should_Throw_SocketClosedException_When_Connection_Is_Lost_Without_Close_Frame()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            var received = new List<int>();

            // server acks the connection, accepts the subscription and sends two next
            // frames, then holds the socket open so the client can abort it without a
            // close frame, simulating a TCP reset, a server crash, or a proxy idle-kill.
            using var testServer = CreateHoldingSubscriptionServer(nextMessageCount: 2);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCts.CancelAfter(TimeSpan.FromSeconds(10));

            // act
            // consume the two buffered next frames, then abort the client socket to force
            // an abnormal connection loss with no close frame.
            await using var enumerator =
                socketResult.ReadResultsAsync().GetAsyncEnumerator(readCts.Token);

            for (var i = 0; i < 2; i++)
            {
                Assert.True(await enumerator.MoveNextAsync());
                received.Add(enumerator.Current.Data.GetProperty("value").GetInt32());
                enumerator.Current.Dispose();
            }

            webSocket.Abort();

            // assert
            var error = await Assert.ThrowsAsync<SocketClosedException>(
                async () => await enumerator.MoveNextAsync());
            Assert.Equal(2, received.Count);
            Assert.Equal((WebSocketCloseStatus)1006, error.Reason);
        });
    }

    [Fact]
    public Task DisposeAsync_Should_Complete_Stream_Cleanly()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            // server acks the connection and accepts the subscription, then holds the
            // socket open without sending any further frames.
            using var testServer = CreateHoldingSubscriptionServer(nextMessageCount: 0);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCts.CancelAfter(TimeSpan.FromSeconds(10));

            // act
            // start enumerating, then tear the client down through its own DisposeAsync,
            // which cancels the client token and must end the stream cleanly.
            var enumeration = Task.Run(
                async () =>
                {
                    await foreach (var result in
                        socketResult.ReadResultsAsync().WithCancellation(readCts.Token))
                    {
                        result.Dispose();
                    }
                },
                ct);

            await client.DisposeAsync();

            // assert
            var finished = await Task.WhenAny(enumeration, Task.Delay(TimeSpan.FromSeconds(5), ct));
            Assert.Same(enumeration, finished);
            await enumeration;
        });
    }

    [Fact]
    public Task ReadResultsAsync_Should_Throw_SocketClosedException_When_Server_Sends_Structurally_Invalid_Message()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            using var testServer = CreateSubscriptionMessageServer(
                _ => """{"type":"next"}""");
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCts.CancelAfter(TimeSpan.FromSeconds(2));

            // act
            async Task ReadResults()
            {
                await foreach (var result in
                    socketResult.ReadResultsAsync().WithCancellation(readCts.Token))
                {
                    result.Dispose();
                }
            }

            // assert
            var error = await Assert.ThrowsAsync<SocketClosedException>(ReadResults);
            Assert.Equal((WebSocketCloseStatus)4400, error.Reason);
        });
    }

    [Fact]
    public Task ReadResultsAsync_Should_Throw_SocketClosedException_When_Server_Sends_Unknown_Message_Type()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            using var testServer = CreateSubscriptionMessageServer(
                id => $$"""{"type":"bogus","id":"{{id}}"}""");
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCts.CancelAfter(TimeSpan.FromSeconds(2));

            // act
            async Task ReadResults()
            {
                await foreach (var result in
                    socketResult.ReadResultsAsync().WithCancellation(readCts.Token))
                {
                    result.Dispose();
                }
            }

            // assert
            var error = await Assert.ThrowsAsync<SocketClosedException>(ReadResults);
            Assert.Equal((WebSocketCloseStatus)4400, error.Reason);
        });
    }

    [Fact]
    public Task ReadResultsAsync_Should_Yield_Result_When_Next_Message_Type_Follows_Payload()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            using var testServer = CreateSubscriptionMessageServer(
                id => $$$$"""{"id":"{{{{id}}}}","payload":{"data":{"event":{"type":"ping"}}},"type":"next"}""");
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCts.CancelAfter(TimeSpan.FromSeconds(2));
            var results = new List<OperationResult>();

            // act
            try
            {
                await foreach (var operationResult in
                    socketResult.ReadResultsAsync().WithCancellation(readCts.Token))
                {
                    results.Add(operationResult);
                    break;
                }
            }
            catch (OperationCanceledException)
                when (readCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
            }

            // assert
            using var result = Assert.Single(results);
            Assert.Equal(
                "ping",
                result.Data.GetProperty("event").GetProperty("type").GetString());
        });
    }

    [Fact(Skip = "This test is flaky. We need to fix it.")]
    public Task Send_Subscribe_SyntaxError()
    {
        var snapshot = new Snapshot();

        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { 123 } }");

            using var testServer = CreateStarWarsServer(output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // assert
            await foreach (var result in socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                Assert.Equal(JsonValueKind.Undefined, result.Data.ValueKind);
                Assert.Equal(JsonValueKind.Array, result.Errors.ValueKind);
                Assert.Equal(JsonValueKind.Undefined, result.Extensions.ValueKind);
                snapshot.Add(result.Errors);
            }

            await snapshot.MatchAsync(ct);
        });
    }

    [Fact]
    public Task Send_Subscribe_ValidationError()
    {
        var snapshot = new Snapshot();

        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { ____ } }");

            using var testServer = CreateStarWarsServer(output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // assert
            await foreach (var result in socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                Assert.Equal(JsonValueKind.Undefined, result.Data.ValueKind);
                Assert.Equal(JsonValueKind.Array, result.Errors.ValueKind);
                Assert.Equal(JsonValueKind.Undefined, result.Extensions.ValueKind);
                snapshot.Add(result.Errors);
            }

            await snapshot.MatchAsync(ct);
        });
    }

    [Fact]
    public Task Send_Connect_With_Auth_Accept()
        => TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using var testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor),
                output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            await SocketClient.ConnectAsync(
                webSocket,
                JsonSerializer.SerializeToElement(new Auth { Token = "abc" }),
                ct);

            // assert
            // no error
        });

    [Fact]
    public Task Send_Connect_With_Auth_Reject()
        => TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using var testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor),
                output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            async Task Connect() => await SocketClient.ConnectAsync(webSocket, ct);

            // assert
            var error = await Assert.ThrowsAsync<SocketClosedException>(Connect);
            Assert.Equal(4403, (int)error.Reason);
        });

    [Fact]
    public Task ConnectAsync_Should_TearDown_Socket_When_Handshake_Is_Canceled()
    {
        return TryTest(async ct =>
        {
            // arrange
            // the server accepts the socket but never sends a connection_ack, so the client's
            // handshake blocks on the acknowledgement until the caller cancels it.
            using var testServer = CreateSilentHandshakeServer();
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(TimeSpan.FromMilliseconds(250));

            // act
            async Task Connect() => await SocketClient.ConnectAsync(webSocket, connectCts.Token);

            // assert
            // the handshake is canceled and the client tears down the socket instead of leaving
            // the fire-and-forget receive pipeline running against an open socket.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(Connect);
            Assert.NotEqual(WebSocketState.Open, webSocket.State);
        });
    }

    private TestServer CreateSilentHandshakeServer()
        => ServerFactory.Create(
            services => services.AddRouting(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(
                    endpoints => endpoints.MapGet(
                        "/graphql",
                        HandleSilentHandshakeAsync)));

    private static async Task HandleSilentHandshakeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync(
            WellKnownProtocols.GraphQL_Transport_WS);
        var ct = context.RequestAborted;
        var buffer = new byte[4096];

        try
        {
            // drain client frames but never acknowledge the connection, so the client handshake
            // blocks until it is canceled and tears the socket down.
            while (socket.State == WebSocketState.Open)
            {
                var (type, _) = await ReceiveClientMessageAsync(socket, buffer, ct);

                if (type is null)
                {
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            // the client aborted the socket when the handshake was canceled, which faults the
            // pending server-side receive.
        }
    }

    private TestServer CreateClosingSubscriptionServer(int nextMessageCount)
        => ServerFactory.Create(
            services => services.AddRouting(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(
                    endpoints => endpoints.MapGet(
                        "/graphql",
                        context => HandleSubscriptionThenCloseAsync(context, nextMessageCount))));

    private TestServer CreateHoldingSubscriptionServer(int nextMessageCount)
        => ServerFactory.Create(
            services => services.AddRouting(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(
                    endpoints => endpoints.MapGet(
                        "/graphql",
                        context => HandleSubscriptionThenHoldAsync(context, nextMessageCount))));

    private TestServer CreateSubscriptionMessageServer(Func<string, string> createMessage)
        => ServerFactory.Create(
            services => services.AddRouting(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(
                    endpoints => endpoints.MapGet(
                        "/graphql",
                        context => HandleSubscriptionMessageAsync(context, createMessage))));

    private static async Task HandleSubscriptionMessageAsync(
        HttpContext context,
        Func<string, string> createMessage)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync(
            WellKnownProtocols.GraphQL_Transport_WS);
        var ct = context.RequestAborted;
        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open)
        {
            var (type, id) = await ReceiveClientMessageAsync(socket, buffer, ct);

            switch (type)
            {
                case "connection_init":
                    await SendServerMessageAsync(socket, """{"type":"connection_ack"}""", ct);
                    break;

                case "ping":
                    await SendServerMessageAsync(socket, """{"type":"pong"}""", ct);
                    break;

                case "subscribe":
                    await SendServerMessageAsync(
                        socket,
                        createMessage(id ?? throw new InvalidOperationException("Subscription id is required.")),
                        ct);
                    break;

                case null:
                    return;
            }
        }
    }

    private static async Task HandleSubscriptionThenCloseAsync(
        HttpContext context,
        int nextMessageCount)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync(
            WellKnownProtocols.GraphQL_Transport_WS);
        var ct = context.RequestAborted;
        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open)
        {
            var (type, id) = await ReceiveClientMessageAsync(socket, buffer, ct);

            switch (type)
            {
                case "connection_init":
                    await SendServerMessageAsync(socket, """{"type":"connection_ack"}""", ct);
                    break;

                case "ping":
                    await SendServerMessageAsync(socket, """{"type":"pong"}""", ct);
                    break;

                case "subscribe":
                    for (var i = 0; i < nextMessageCount; i++)
                    {
                        var next = JsonSerializer.Serialize(
                            new { type = "next", id, payload = new { data = new { value = i } } });
                        await SendServerMessageAsync(socket, next, ct);
                    }

                    // graphql-transport-ws server terminates the connection with a close
                    // frame (1012 Service Restart) while the subscription is still active
                    // and without sending a complete message.
                    await socket.CloseOutputAsync((WebSocketCloseStatus)1012, "Service Restart", ct);
                    return;

                case null:
                    return;
            }
        }
    }

    private static async Task HandleSubscriptionThenHoldAsync(
        HttpContext context,
        int nextMessageCount)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync(
            WellKnownProtocols.GraphQL_Transport_WS);
        var ct = context.RequestAborted;
        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var (type, id) = await ReceiveClientMessageAsync(socket, buffer, ct);

                switch (type)
                {
                    case "connection_init":
                        await SendServerMessageAsync(socket, """{"type":"connection_ack"}""", ct);
                        break;

                    case "ping":
                        await SendServerMessageAsync(socket, """{"type":"pong"}""", ct);
                        break;

                    case "subscribe":
                        for (var i = 0; i < nextMessageCount; i++)
                        {
                            var next = JsonSerializer.Serialize(
                                new { type = "next", id, payload = new { data = new { value = i } } });
                            await SendServerMessageAsync(socket, next, ct);
                        }

                        // hold the socket open without sending a complete or close frame so
                        // the client can abort it to simulate an abnormal connection loss.
                        break;

                    case null:
                        return;
                }
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            // the client aborted the socket to simulate an abnormal connection loss, which
            // faults the pending server-side receive.
        }
    }

    private static async Task<(string? Type, string? Id)> ReceiveClientMessageAsync(
        WebSocket socket,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return (null, null);
            }

            stream.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        stream.Position = 0;
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString();
        var id = root.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;
        return (type, id);
    }

    private static Task SendServerMessageAsync(
        WebSocket socket,
        string message,
        CancellationToken cancellationToken)
        => socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);

    private class AuthInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            var payload = connectionInitMessage.Payload?.Deserialize<Auth>();

            if (payload?.Token is not null)
            {
                return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
            }

            return new(ConnectionStatus.Reject());
        }
    }

    private sealed class Auth
    {
        public string? Token { get; set; }
    }
}
