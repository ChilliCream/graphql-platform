using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.AspNetCore.Subscriptions.GraphQLOverWebSocket;

public class WebSocketProtocolTests : SubscriptionTestBase
{
    public WebSocketProtocolTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task Send_Connect_Accept()
    {
        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal(Messages.ConnectionAccept, message![MessageProperties.Type]);
        });
    }

    [Fact]
    public Task Send_Connect_With_Auth_Accept()
    {
        return TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using TestServer testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitAsync(new() { ["token"] = "abc " }, ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal(Messages.ConnectionAccept, message![MessageProperties.Type]);
        });
    }

    [Fact]
    public Task Send_Connect_With_Auth_Reject()
    {
        return TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using TestServer testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitAsync(ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(CloseReasons.Unauthorized, (int)webSocket.CloseStatus!.Value);
        });
    }


    [Fact]
    public Task Send_Connect_Accept_Explicit_Route()
    {
        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateServer(b => b.MapGraphQLWebSocket());
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(
                new("ws://localhost:5000/graphql/ws"),
                ct);

            // act
            await webSocket.SendConnectionInitAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message!["type"]);
        });
    }

    [Fact]
    public Task Send_Connect_Accept_Explicit_Route_Explicit_Path()
    {
        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateServer(b => b.MapGraphQLWebSocket("/foo/bar"));
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client.ConnectAsync(
                new("ws://localhost:5000/foo/bar"),
                ct);

            // act
            await webSocket.SendConnectionInitAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message!["type"]);
        });
    }

    [Fact]
    public Task Connect_With_Invalid_Protocol()
    {
        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = testServer.CreateWebSocketClient();

            // act
            client.ConfigureRequest = r => r.Headers.Add("Sec-WebSocket-Protocol", "foo");
            WebSocket socket = await client.ConnectAsync(SubscriptionUri, ct);

            // assert
            await socket.ReceiveServerMessageAsync(ct);
            Assert.True(socket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.ProtocolError, socket.CloseStatus!.Value);
        });
    }

    [Fact]
    public Task Subscribe_ReceiveDataOnMutation()
    {
        SnapshotFullName snapshotName = Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            var payload = new SubscribePayload(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

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

            IReadOnlyDictionary<string, object> message =
                await WaitForMessage(
                    webSocket,
                    "data",
                    TimeSpan.FromSeconds(15),
                    ct);

            Assert.NotNull(message);
            Snapshot.Match(message, snapshotName);
        });
    }

    [Fact]
    public Task Send_Subscribe_Complete()
    {
        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            var payload = new SubscribePayload(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            const string subscriptionId = "abc";
            await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

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

            await WaitForMessage(webSocket, Messages.Next, ct);

            // act
            await webSocket.SendCompleteAsync(subscriptionId, ct);

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
            var message = await WaitForMessage(webSocket, Messages.Next, ct);
            Assert.Null(message);
        });
    }

    [Fact]
    public Task Send_Start_SyntaxError()
    {
        SnapshotFullName snapshotName = Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            var payload = new SubscribePayload(
                "subscription { onReview(episode: NEW_HOPE) { 123 } }");
            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

            // assert
            var message = await WaitForMessage(webSocket, Messages.Error, ct);
            Assert.NotNull(message);
            Snapshot.Match(message, snapshotName);
        });
    }

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

            var payload = new SubscribePayload(
                "subscription { onReview(episode: NEW_HOPE) { _stars } }");
            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

            // assert
            var message = await WaitForMessage(webSocket, Messages.Error, ct);
            Assert.NotNull(message);
            Snapshot.Match(message, snapshotName);
        });
    }

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
