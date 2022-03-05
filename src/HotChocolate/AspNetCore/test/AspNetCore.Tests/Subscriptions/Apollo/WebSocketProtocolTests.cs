using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions.Apollo;

public class WebSocketProtocolTests : SubscriptionTestBase
{
    public WebSocketProtocolTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive()
    {
        return TryTest(async ct =>
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
    }

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive_Explicit_Route()
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
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message["type"]);
        });
    }

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive_Explicit_Route_Explicit_Path()
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
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message["type"]);
        });
    }

    [Fact]
    public Task Send_Terminate()
    {
        return TryTest(async ct =>
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
            Assert.Equal(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatus.Value);
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

            client.ConfigureRequest = r => r.Headers.Add("Sec-WebSocket-Protocol", "foo");

            // act
            WebSocket socket = await client.ConnectAsync(SubscriptionUri, ct);
            var buffer = new byte[1024];
            await socket.ReceiveAsync(buffer, ct);

            // assert
            Assert.True(socket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.ProtocolError, socket.CloseStatus.Value);
        });
    }

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
    public Task Send_Start_Stop()
    {
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

            await WaitForMessage(webSocket, "data", TimeSpan.FromSeconds(15), ct);

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
    }
}
