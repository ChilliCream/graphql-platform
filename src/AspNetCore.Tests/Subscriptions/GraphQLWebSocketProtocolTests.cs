
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
            new Uri("ws://localhost:5000/subscriptions");

        private TestServerFactory TestServerFactory { get; }

        [Fact]
        public async Task Send_Connect_AcceptAndKeepAllive()
        {
            // arrange
            TestServer testServer = CreateTestServer();
            WebSocketClient client = testServer.CreateWebSocketClient();
            WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, CancellationToken.None);

            // act and assert
            await ConnectAsync(webSocket);
        }

        [Fact]
        public async Task Send_Start_ReceiveDataOnMutation()
        {
            // arrange
            TestServer testServer = CreateTestServer();
            WebSocketClient client = testServer.CreateWebSocketClient();
            WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, CancellationToken.None);
            await ConnectAsync(webSocket);

            SubscriptionQuery query = new SubscriptionQuery
            {
                Query = "subscription { foo }"
            };

            // act
            string id = await webSocket.SendSubscriptionStartAsync(query);

            // assert
            await testServer.SendRequestAsync(new QueryRequestDto
            {
                Query = "mutation { sendFoo }"
            });

            GenericOperationMessage message =
                await webSocket.ReceiveServerMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageTypes.Subscription.Data, message.Type);

            var result = message.Payload.ToObject<Dictionary<string, object>>();
            Assert.True(result.ContainsKey("data"));
        }

        private TestServer CreateTestServer()
        {
            return TestServerFactory.Create(
                c =>
                {
                    c.RegisterMutationType<Mutation>();
                    c.RegisterSubscriptionType<Subscription>();
                },
                s =>
                {
                    var eventRegistry = new InMemoryEventRegistry();
                    s.AddSingleton<IEventRegistry>(eventRegistry);
                    s.AddSingleton<IEventSender>(eventRegistry);
                },
                null);
        }

        private async Task ConnectAsync(WebSocket webSocket)
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
    }
}
