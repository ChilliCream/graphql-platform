using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.Tests;
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
    public Task Subscribe_Disconnect()
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

            // ... disconnect
            webSocket.Abort();

            // assert
            // ... try iterate
            await foreach (var unused in socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                Assert.Fail("Stream should have been aborted");
            }
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
            Assert.Equal(4401, (int)error.Reason);
        });

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
