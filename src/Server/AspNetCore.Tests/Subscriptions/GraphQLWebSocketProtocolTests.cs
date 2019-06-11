using System.Diagnostics;

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class GraphQLWebSocketProtocolTests
        : IClassFixture<TestServerFactory>
    {
        public GraphQLWebSocketProtocolTests(TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        private Uri SubscriptionUri { get; } =
            new Uri("ws://localhost:5000/ws");

        private TestServerFactory TestServerFactory { get; }

        [Fact]
        public async Task Send_Connect_AcceptAndKeepAlive()
        {
            // arrange
            TestServer testServer = CreateTestServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client
                .ConnectAsync(SubscriptionUri, CancellationToken.None);

            // act and assert
            await SessionInitializeAsync(webSocket);
        }

        [Fact(Skip = "FIX: This test still does not run on the build server.")]
        public async Task Send_Start_ReceiveDataOnMutation()
        {
            // arrange
            TestServer testServer = CreateTestServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await client
                .ConnectAsync(SubscriptionUri, CancellationToken.None);

            await SessionInitializeAsync(webSocket);

            var query = new SubscriptionQuery
            {
                Query = "subscription { foo }"
            };

            // act
            string id = await webSocket.SendSubscriptionStartAsync(query);

            // assert
            await testServer.SendRequestAsync(new ClientQueryRequest
            {
                Query = "mutation { sendFoo }"
            });

            GenericOperationMessage message =
                await WaitForMessage(
                    webSocket,
                    MessageTypes.Subscription.Data,
                    TimeSpan.FromSeconds(10));

            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Subscription.Data, message.Type);

            Dictionary<string, object> result = message.Payload
                .ToObject<Dictionary<string, object>>();
            Assert.True(result.ContainsKey("data"));
        }

        private async Task<GenericOperationMessage> WaitForMessage(
            WebSocket webSocket, string messageType, TimeSpan timeout)
        {
            Stopwatch timer = Stopwatch.StartNew();

            try
            {
                while (timer.Elapsed <= timeout)
                {
                    GenericOperationMessage message =
                        await webSocket.ReceiveServerMessageAsync();

                    if (messageType.Equals(message?.Type))
                    {
                        return message;
                    }

                    if (message != null
                        && !MessageTypes.Connection.KeepAlive.Equals(
                            message.Type))
                    {
                        throw new Exception(
                            $"Unexpected message type: {message.Type}");
                    }
                }
            }
            finally
            {
                timer.Stop();
            }

            return null;
        }

        private TestServer CreateTestServer()
        {
            return TestServerFactory.Create(
                c =>
                {
                    c.RegisterQueryType<Query>();
                    c.RegisterMutationType<Mutation>();
                    c.RegisterSubscriptionType<Subscription>();
                },
                s => s.AddInMemorySubscriptionProvider(),
                new QueryMiddlewareOptions());
        }

        private async Task SessionInitializeAsync(WebSocket webSocket)
        {
            // act
            await webSocket.SendConnectionInitializeAsync();

            // assert
            GenericOperationMessage message =
                await webSocket.ReceiveServerMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Connection.Accept, message.Type);

            message = await webSocket.ReceiveServerMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Connection.KeepAlive, message.Type);
        }

        private static WebSocketClient CreateWebSocketClient(TestServer testServer)
        {
            WebSocketClient client = testServer.CreateWebSocketClient();

            client.ConfigureRequest = r =>
            {
                r.Headers.Add("Sec-WebSocket-Protocol", "graphql-ws");
            };

            return client;
        }

        public class Mutation
        {
            public async Task<string> SendFoo([Service]IEventSender sender)
            {
                await sender.SendAsync(new EventMessage("foo"));
                return "sendBar";
            }
        }

        public class Subscription
        {
            public string Foo() => "bar";
        }

        public class Query
        {
            public string Foo { get; set; }
        }
    }
}
