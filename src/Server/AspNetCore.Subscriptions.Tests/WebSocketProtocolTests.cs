using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;
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

        private Uri SubscriptionUri { get; } =
           new Uri("ws://localhost:5000/ws");


        [Fact]
        public async Task Send_Connect_AcceptAndKeepAlive()
        {
            // arrange
            TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client
                .ConnectAsync(SubscriptionUri, CancellationToken.None);

            // act
            await webSocket.SendConnectionInitializeAsync();

            // assert
            IReadOnlyDictionary<string, object> message =
                await webSocket.ReceiveServerMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Connection.Accept, message["type"]);

            message = await webSocket.ReceiveServerMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Connection.KeepAlive, message["type"]);
        }

        [Fact]
        public async Task Send_Terminate()
        {
            // arrange
            TestServer testServer = CreateStarWarsServer();
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

        [Fact]
        public async Task Connect_With_Invalid_Protocol()
        {
            // arrange
            TestServer testServer = CreateStarWarsServer();

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

        [Fact]
        public async Task Send_Start_ReceiveDataOnMutation()
        {
            // arrange
            TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);

            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEWHOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // assert
            await testServer.SendRequestAsync(new ClientQueryRequest
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
            message.MatchSnapshot();
        }

        [Fact]
        public async Task Send_Start_Stop()
        {
            // arrange
            TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);

            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEWHOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            await testServer.SendRequestAsync(new ClientQueryRequest
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

            await testServer.SendRequestAsync(new ClientQueryRequest
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

        [Fact]
        public async Task Send_Start_ReceiveDataOnMutation_Large_Message()
        {
            // arrange
            TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);

            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEWHOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscriptionStartAsync(
                subscriptionId, request, true);

            // assert
            await webSocket.SendEmptyMessageAsync();

            await testServer.SendRequestAsync(new ClientQueryRequest
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
            message.MatchSnapshot();
        }

        private async Task<IReadOnlyDictionary<string, object>> WaitForMessage(
            WebSocket webSocket, string type, TimeSpan timeout)
        {
            Stopwatch timer = Stopwatch.StartNew();

            try
            {
                while (timer.Elapsed <= timeout)
                {
                    await Task.Delay(50);

                    IReadOnlyDictionary<string, object> message =
                        await webSocket.ReceiveServerMessageAsync();

                    if (message != null && type.Equals(message["type"]))
                    {
                        return message;
                    }

                    if (message != null
                        && !MessageTypes.Connection.KeepAlive.Equals(
                            message["type"]))
                    {
                        throw new InvalidOperationException(
                            $"Unexpected message type: {message["type"]}");
                    }
                }
            }
            finally
            {
                timer.Stop();
            }

            return null;
        }

        private async Task<WebSocket> ConnectToServerAsync(
            WebSocketClient client)
        {
            WebSocket webSocket = await client.ConnectAsync(
                SubscriptionUri, CancellationToken.None);

            await webSocket.SendConnectionInitializeAsync();

            IReadOnlyDictionary<string, object> message =
                await webSocket.ReceiveServerMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Connection.Accept, message["type"]);

            message = await webSocket.ReceiveServerMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Connection.KeepAlive, message["type"]);

            return webSocket;
        }

        private static WebSocketClient CreateWebSocketClient(
            TestServer testServer)
        {
            WebSocketClient client = testServer.CreateWebSocketClient();

            client.ConfigureRequest = r =>
            {
                r.Headers.Add("Sec-WebSocket-Protocol", "graphql-ws");
            };

            return client;
        }
    }
}
