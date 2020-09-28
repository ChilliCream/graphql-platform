using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketProtocolTests
        : SubscriptionTestBase
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
                using (TestServer testServer = CreateStarWarsServer())
                {
                    // arrange
                    WebSocketClient client = CreateWebSocketClient(testServer);
                    WebSocket webSocket = await client
                        .ConnectAsync(SubscriptionUri, CancellationToken.None);

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
                }
            });
        }

        [Fact]
        public Task Send_Terminate()
        {
            return TryTest(async () =>
            {
                using (TestServer testServer = CreateStarWarsServer())
                {
                    // arrange
                    WebSocketClient client = CreateWebSocketClient(testServer);
                    WebSocket webSocket = await ConnectToServerAsync(client);

                    // act
                    await webSocket.SendTerminateConnectionAsync();

                    // assert
                    byte[] buffer = new byte[1024];
                    await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                    Assert.True(webSocket.CloseStatus.HasValue);
                    Assert.Equal(
                        WebSocketCloseStatus.NormalClosure,
                        webSocket.CloseStatus.Value);
                }
            });
        }

        [Fact]
        public Task Connect_With_Invalid_Protocol()
        {
            return TryTest(async () =>
            {
                using (TestServer testServer = CreateStarWarsServer())
                {
                    // arrange
                    WebSocketClient client = testServer.CreateWebSocketClient();

                    client.ConfigureRequest = r =>
                    {
                        r.Headers.Add("Sec-WebSocket-Protocol", "foo");
                    };

                    // act
                    WebSocket socket = await client.ConnectAsync(
                        SubscriptionUri,
                        CancellationToken.None);

                    byte[] buffer = new byte[1024];
                    await socket.ReceiveAsync(buffer, CancellationToken.None);

                    // assert
                    Assert.True(socket.CloseStatus.HasValue);
                    Assert.Equal(
                        WebSocketCloseStatus.ProtocolError,
                        socket.CloseStatus.Value);
                }
            });
        }

        [Fact]
        public Task Send_Start_ReceiveDataOnMutation()
        {
            SnapshotFullName snapshotName = Snapshot.FullName();

            return TryTest(async () =>
            {
                using (TestServer testServer = CreateStarWarsServer())
                {
                    // arrange
                    WebSocketClient client = CreateWebSocketClient(testServer);
                    WebSocket webSocket = await ConnectToServerAsync(client);

                    var document = Utf8GraphQLParser.Parse(
                        "subscription { onReview(episode: NEWHOPE) { stars } }");

                    var request = new GraphQLRequest(document);

                    const string subscriptionId = "abc";

                    // act
                    await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                    // assert
                    await testServer.SendPostRequestAsync(new ClientQueryRequest
                    {
                        Query = @"
                    mutation {
                        createReview(episode:NEWHOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }
                "
                    });

                    IReadOnlyDictionary<string, object> message =
                        await WaitForMessage(
                            webSocket,
                            MessageTypes.Subscription.Data,
                            TimeSpan.FromSeconds(15));

                    Assert.NotNull(message);
                    Snapshot.Match(message, snapshotName);
                }
            });
        }

        [Fact]
        public Task Send_Start_Stop()
        {
            return TryTest(async () =>
            {
                using (TestServer testServer = CreateStarWarsServer())
                {
                    // arrange
                    WebSocketClient client = CreateWebSocketClient(testServer);
                    WebSocket webSocket = await ConnectToServerAsync(client);

                    var document = Utf8GraphQLParser.Parse(
                        "subscription { onReview(episode: NEWHOPE) { stars } }");

                    var request = new GraphQLRequest(document);

                    const string subscriptionId = "abc";

                    await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                    await testServer.SendPostRequestAsync(new ClientQueryRequest
                    {
                        Query = @"
                    mutation {
                        createReview(episode:NEWHOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }
                "
                    });

                    IReadOnlyDictionary<string, object> message =
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
                        createReview(episode:NEWHOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }
                "
                    });

                    message = await WaitForMessage(
                        webSocket,
                        MessageTypes.Subscription.Data,
                        TimeSpan.FromSeconds(5));

                    // assert
                    Assert.Null(message);
                }
            });
        }

        private static async Task TryTest(Func<Task> action)
        {
            // we will try four times ....
            int count = 0;
            int wait = 50;

            while (true)
            {
                if (count < 3)
                {
                    try
                    {
                        await action().ConfigureAwait(false);
                        break;
                    }
                    catch
                    {
                        // try again
                    }
                }
                else
                {
                    await action().ConfigureAwait(false);
                    break;
                }

                await Task.Delay(wait).ConfigureAwait(false);
                wait = wait * 2;
                count++;
            }
        }
    }
}
