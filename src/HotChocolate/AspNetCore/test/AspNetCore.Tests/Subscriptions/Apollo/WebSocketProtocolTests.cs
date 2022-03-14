using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.AspNetCore.Subscriptions.Apollo;

public class WebSocketProtocolTests : SubscriptionTestBase
{
    public WebSocketProtocolTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message["type"]);
        });

    [Fact]
    public Task Send_Connect_To_Many_Init_Messages()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            await webSocket.SendConnectionInitializeAsync(ct);
            await WaitForMessage(webSocket, "connection_ack", ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.ProtocolError, webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task No_ConnectionInit_Timeout()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer(
                configureConventions: mapping => mapping.WithOptions(
                    new GraphQLServerOptions { Sockets =
                    {
                        ConnectionInitializationTimeout = TimeSpan.FromMilliseconds(50),
                        KeepAliveInterval = TimeSpan.FromMilliseconds(150)
                    }}));
            WebSocketClient client = CreateWebSocketClient(testServer);

            // act
            using WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(WebSocketCloseStatus.ProtocolError, webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Connect_With_Auth_Accept()
        => TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using TestServer testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitAsync(new() { ["token"] = "abc " }, ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message![MessageProperties.Type]);
        });

    [Fact]
    public Task Send_Connect_With_Auth_Reject()
        => TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using TestServer testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitAsync(ct);

            // assert
            await WaitForMessage(webSocket, "connection_error", ct);
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive_Explicit_Route()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateServer(b => b.MapGraphQLWebSocket());
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(
                new("ws://localhost:5000/graphql/ws"),
                ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message["type"]);
        });

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive_Explicit_Route_Explicit_Path()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateServer(b => b.MapGraphQLWebSocket("/foo/bar"));
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(
                new("ws://localhost:5000/foo/bar"),
                ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message["type"]);
        });

    [Fact]
    public Task Send_Terminate()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            // act
            await webSocket.SendTerminateConnectionAsync(ct);

            // assert
            var buffer = new byte[1024];
            await webSocket.ReceiveAsync(buffer, ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Connect_With_Invalid_Protocol()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = testServer.CreateWebSocketClient();

            client.ConfigureRequest = r => r.Headers.Add("Sec-WebSocket-Protocol", "foo");

            // act
            WebSocket socket = await client.ConnectAsync(SubscriptionUri, ct);
            var buffer = new byte[1024];
            await socket.ReceiveAsync(buffer, ct);

            // assert
            Assert.True(socket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.ProtocolError, socket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Start_ReceiveDataOnMutation()
    {
        SnapshotFullName snapshotName = Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            var request = new GraphQLRequest(document);
            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // assert
            await testServer.SendPostRequestAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        mutation {
                            createReview(episode: NEW_HOPE review: {
                                commentary: ""foo""
                                stars: 5
                            }) {
                                stars
                            }
                        }"
                });

            var message = await WaitForMessage(webSocket, "data", ct);
            Assert.NotNull(message);
            Snapshot.Match(message, snapshotName);
        });
    }

    [Fact]
    public Task Send_Start_Id_Not_Unique()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            var request = new GraphQLRequest(document);
            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.InternalServerError, webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Subscribe_No_Id()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await ConnectToServerAsync(client, ct);

            // act

            await webSocket.SendMessageAsync("{ \"type\": \"subscribe\" }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Subscribe_Empty_Id()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await ConnectToServerAsync(client, ct);

            // act

            await webSocket.SendMessageAsync("{ \"type\": \"subscribe\", \"id\": \"\" }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Subscribe_SyntaxError()
    {
        SnapshotFullName snapshotName = Snapshot.FullName();

        return TryTest(
            async ct =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync(
                    @"{
                    ""type"": ""start"",
                    ""id"": ""abc"",
                    ""payload"": {
                        ""query"": ""}""
                    }
                }",
                    ct);

                // assert
                var message = await WaitForMessage(webSocket, "error", ct);
                message.MatchSnapshot(snapshotName);
            });
    }

    [Fact]
    public Task Send_Subscribe_SyntaxError_No_Id()
        => TryTest(
            async ct =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync(
                    @"{
                    ""type"": ""start"",
                    ""id"": """",
                    ""payload"": {
                        ""query"": ""}""
                    }
                }",
                    ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Start_Stop()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            await testServer.SendPostRequestAsync(new ClientQueryRequest
            {
                Query = @"
                    mutation {
                        createReview(episode:NEW_HOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }"
            });

            await WaitForMessage(webSocket, "data", ct);

            // act
            await webSocket.SendSubscriptionStopAsync(subscriptionId, ct);

            await testServer.SendPostRequestAsync(new ClientQueryRequest
            {
                Query = @"
                    mutation {
                        createReview(episode:NEW_HOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }"
            });

            // assert
            var message = await WaitForMessage(webSocket, "data", TimeSpan.FromSeconds(5), ct);
            Assert.Null(message);
        });

    [Fact]
    public Task Send_Start_ValidationError()
    {
        SnapshotFullName snapshotName = Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { _stars } }");
            var request = new GraphQLRequest(document);
            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // assert
            var message = await WaitForMessage(webSocket, "error", ct);
            Assert.NotNull(message);
            Snapshot.Match(message, snapshotName);
        });
    }

    [Fact]
    public Task Send_Invalid_Message_String()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendMessageAsync("hello", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(WebSocketCloseStatus.InternalServerError, webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Invalid_Message_No_Type()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendMessageAsync("{ }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Invalid_Message_Invalid_Type()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await ConnectToServerAsync(client, ct);

            // act
            await webSocket.SendMessageAsync("{ \"type\": \"abc\" }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus!.Value);
        });

    [Fact]
    public Task Send_Invalid_Message_Not_An_Object()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendMessageAsync("[]", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus!.Value);
        });

    private class AuthInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            Auth? payload = connectionInitMessage.As<Auth>();

            if (payload?.Token is not null)
            {
                return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
            }

            return new(ConnectionStatus.Reject());
        }

        private sealed class Auth
        {
            public string? Token { get; set; }
        }
    }
}
