using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketProtocolTests : SubscriptionTestBase
    {
        public WebSocketProtocolTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public Task Send_Connect_AcceptAndKeepAlive()
        {
            return TryTest(async () =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = CreateWebSocketClient(testServer);
                WebSocket webSocket =
                    await client.ConnectAsync(SubscriptionUri, CancellationToken.None);

                // act
                await webSocket.SendConnectionInitializeAsync();

                // assert
                IReadOnlyDictionary<string, object> message =
                    await webSocket.ReceiveServerMessageAsync();
                Assert.NotNull(message);
                Assert.Equal(
                    MessageTypes.Connection.Accept,
                    message["type"]);

                message = await webSocket.ReceiveServerMessageAsync();
                Assert.NotNull(message);
                Assert.Equal(
                    MessageTypes.Connection.KeepAlive,
                    message["type"]);
            });
        }

        [Fact]
        public Task Send_Terminate()
        {
            return TryTest(async () =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = CreateWebSocketClient(testServer);
                WebSocket webSocket = await ConnectToServerAsync(client);

                // act
                await webSocket.SendTerminateConnectionAsync();

                // assert
                var buffer = new byte[1024];
                await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(
                    WebSocketCloseStatus.NormalClosure,
                    webSocket.CloseStatus.Value);
            });
        }

        [Fact]
        public Task Connect_With_Invalid_Protocol()
        {
            return TryTest(async () =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = testServer.CreateWebSocketClient();

                client.ConfigureRequest = r =>
                {
                    r.Headers.Add("Sec-WebSocket-Protocol", "foo");
                };

                // act
                WebSocket socket = await client.ConnectAsync(
                    SubscriptionUri,
                    CancellationToken.None);

                var buffer = new byte[1024];
                await socket.ReceiveAsync(buffer, CancellationToken.None);

                // assert
                Assert.True(socket.CloseStatus.HasValue);
                Assert.Equal(
                    WebSocketCloseStatus.ProtocolError,
                    socket.CloseStatus.Value);
            });
        }

        [Fact(Skip = "TODO: This test is flaky")]
        public Task Send_Start_ReceiveDataOnMutation()
        {
            SnapshotFullName snapshotName = Snapshot.FullName();

            return TryTest(async () =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = CreateWebSocketClient(testServer);
                WebSocket webSocket = await ConnectToServerAsync(client);

                DocumentNode document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");

                var request = new GraphQLRequest(document);

                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                // assert
                await testServer.SendPostRequestAsync(new ClientQueryRequest
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
                        MessageTypes.Subscription.Data,
                        TimeSpan.FromSeconds(15));

                Assert.NotNull(message);
                Snapshot.Match(message, snapshotName);
            });
        }

        [Fact]
        public Task Send_Start_Stop()
        {
            return TryTest(async () =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = CreateWebSocketClient(testServer);
                WebSocket webSocket = await ConnectToServerAsync(client);

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

                await WaitForMessage(
                    webSocket,
                    MessageTypes.Subscription.Data,
                    TimeSpan.FromSeconds(15));

                // act
                await webSocket.SendSubscriptionStopAsync(subscriptionId);

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

                IReadOnlyDictionary<string, object> message = await WaitForMessage(
                    webSocket,
                    MessageTypes.Subscription.Data,
                    TimeSpan.FromSeconds(5));

                // assert
                Assert.Null(message);
            });
        }
    }
}
