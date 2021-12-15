using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions;

public class WebSocketInterceptorTests : SubscriptionTestBase
{
    public WebSocketInterceptorTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task Send_ShouldTrigger_OnConnectAsync()
    {
        return TryTest(async () =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);
            TestSocketSessionInterceptor testInterceptor =
                testServer.Services.GetRequiredService<TestSocketSessionInterceptor>();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // act
            await webSocket.SendSubscriptionStopAsync(subscriptionId);
            await WaitForConditions(
                () => testInterceptor.ConnectWasCalled,
                TimeSpan.FromMilliseconds(500));

            // assert
            Assert.True(testInterceptor.ConnectWasCalled);
        });
    }

    [Fact]
    public Task Send_ShouldTrigger_OnRequestAsync()
    {
        return TryTest(async () =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);
            TestSocketSessionInterceptor testInterceptor =
                testServer.Services.GetRequiredService<TestSocketSessionInterceptor>();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // act
            await webSocket.SendSubscriptionStopAsync(subscriptionId);
            await WaitForConditions(
                () => testInterceptor.Requests.Count > 0,
                TimeSpan.FromMilliseconds(500));

            // assert
            Assert.Single(testInterceptor.Requests);
        });
    }

    [Fact]
    public Task Send_ShouldTrigger_OnCloseAsync()
    {
        return TryTest(async () =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);
            TestSocketSessionInterceptor testInterceptor =
                testServer.Services.GetRequiredService<TestSocketSessionInterceptor>();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // act
            await webSocket.SendSubscriptionStopAsync(subscriptionId);
            await webSocket.SendTerminateConnectionAsync();

            await WaitForConditions(
                () => testInterceptor.CloseWasCalled,
                TimeSpan.FromMilliseconds(500));

            // assert
            Assert.True(testInterceptor.CloseWasCalled);
        });
    }

    [Fact]
    public Task Send_ShouldTrigger_OnCloseAsync_InCaseOfException()
    {
        return TryTest(async () =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);
            TestSocketSessionInterceptor testInterceptor =
                testServer.Services.GetRequiredService<TestSocketSessionInterceptor>();

            DocumentNode document =
                Utf8GraphQLParser.Parse("subscription { onException }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // act
            await WaitForConditions(
            () => testInterceptor.CloseWasCalled,
            TimeSpan.FromMilliseconds(500));

            // assert
            Assert.True(testInterceptor.CloseWasCalled);
        });
    }

    [Fact]
    public Task Send_ShouldTrigger_OnCloseAsync_UserClose()
    {
        return TryTest(async () =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);
            TestSocketSessionInterceptor testInterceptor =
                testServer.Services.GetRequiredService<TestSocketSessionInterceptor>();

            DocumentNode document =
                Utf8GraphQLParser.Parse("subscription { onNext }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            var cts = new CancellationTokenSource();
            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token);

            // act
            await WaitForConditions(
            () => testInterceptor.CloseWasCalled,
            TimeSpan.FromMilliseconds(500));

            cts.Cancel();

            // assert
            Assert.True(testInterceptor.CloseWasCalled);
        });
    }

    [Fact]
    public Task Should_InjectServicesIntoInterceptor()
    {
        return TryTest(async () =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer(configureServices: sp =>
        {
            sp.AddSingleton<ExampleService>();
            sp
                .AddGraphQLServer()
                .AddSocketSessionInterceptor<SocketInterceptor>();
        });
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client);
            TestSocketSessionInterceptor testInterceptor =
                testServer.Services.GetRequiredService<TestSocketSessionInterceptor>();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // act
            await webSocket.SendSubscriptionStopAsync(subscriptionId);

            // assert
            Assert.NotNull(SocketInterceptor.Instance.Service);
        });
    }

    public class ExampleService
    {
    }

    public class SocketInterceptor : DefaultSocketSessionInterceptor
    {
        public static SocketInterceptor Instance;

        public SocketInterceptor(ExampleService service)
        {
            Service = service;
            Instance = this;
        }

        public ExampleService Service { get; }
    }
}
