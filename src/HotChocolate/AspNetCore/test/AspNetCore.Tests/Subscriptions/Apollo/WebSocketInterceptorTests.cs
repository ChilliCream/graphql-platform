using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using Xunit;

#nullable enable

namespace HotChocolate.AspNetCore.Subscriptions.Apollo;

public class WebSocketInterceptorTests : SubscriptionTestBase
{
    public WebSocketInterceptorTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task InitMessage_Should_Trigger_OnConnectAsync()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);

                // act
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                // assert
                await WaitForConditions(() => interceptor.OnConnectInvoked, ct);
                Assert.True(interceptor.OnConnectInvoked);
            });

    [Fact]
    public Task SubscribeMessage_Should_Trigger_OnRequestAsync()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                DocumentNode document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                // assert
                await webSocket.SendSubscriptionStopAsync(subscriptionId, ct);
                await WaitForConditions(() => interceptor.OnRequestInvoked, ct);
                Assert.True(interceptor.OnRequestInvoked);
            });

    [Fact]
    public Task Event_Should_Trigger_OnResultAsync()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                DocumentNode document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";
                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                // act
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

                // assert
                await WaitForConditions(() => interceptor.OnResultInvoked, ct);
                Assert.True(interceptor.OnResultInvoked);
                Assert.Equal(1, interceptor.OnResultCount);
            });

    [Fact]
    public Task Event_Should_Trigger_OnResult_Two_Events_Async()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                DocumentNode document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";
                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                // act
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

                // assert
                await WaitForConditions(() => interceptor.OnResultInvoked, ct);
                Assert.True(interceptor.OnResultInvoked);
                Assert.Equal(2, interceptor.OnResultCount);
            });

    [Fact]
    public Task StopMessage_Should_Trigger_OnCompleteAsync()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                DocumentNode document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";

                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                // act
                await webSocket.SendSubscriptionStopAsync(subscriptionId, ct);

                // assert
                await WaitForConditions(() => interceptor.OnCompleteInvoked, ct);
                Assert.True(interceptor.OnCompleteInvoked);
            });

    [Fact]
    public Task TerminateMessage_Should_Trigger_OnCloseAsync()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                DocumentNode document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";

                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);
                await webSocket.SendSubscriptionStopAsync(subscriptionId, ct);

                // act
                await webSocket.SendTerminateConnectionAsync(ct);

                // assert
                await WaitForConditions(() => interceptor.OnCloseInvoked, ct);
                Assert.True(interceptor.OnCloseInvoked);
            });

    [Fact]
    public Task SubscriptionError_Should_Trigger_OnComplete()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                DocumentNode document = Utf8GraphQLParser.Parse("subscription { onException }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                // assert
                await WaitForConditions(() => interceptor.OnCompleteInvoked, ct);
                Assert.True(interceptor.OnCompleteInvoked, "Completed");
            });

    [Fact]
    public Task WebSocketClose_Should_Trigger_OnComplete_And_OnClose()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new TestSocketSessionInterceptor();
                using TestServer testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                WebSocketClient client = CreateWebSocketClient(testServer);
                using WebSocket webSocket = await ConnectToServerAsync(client, ct);

                DocumentNode document = Utf8GraphQLParser.Parse("subscription { onNext }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";

                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);
                using var cts = new CancellationTokenSource();

                void BeginClose()
                    => Task.Factory.StartNew(
                        () => webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "",
                            cts.Token),
                        default,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);

                // act
                BeginClose();

                // assert
                await WaitForConditions(() => interceptor.OnCloseInvoked, ct);
                cts.Cancel();

                Assert.True(interceptor.OnCompleteInvoked, "OnCompleteInvoked");
                Assert.True(interceptor.OnCloseInvoked, "OnCloseInvoked");
            });

    public class TestSocketSessionInterceptor : DefaultSocketSessionInterceptor
    {
        public bool OnConnectInvoked { get; private set; }

        public bool OnRequestInvoked { get; private set; }

        public bool OnResultInvoked { get; private set; }

        public int OnResultCount { get; private set; }

        public bool OnCompleteInvoked { get; private set; }

        public bool OnCloseInvoked { get; private set; }

        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            OnConnectInvoked = true;
            return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
        }

        public override ValueTask OnRequestAsync(
            ISocketSession session,
            string operationSessionId,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken = default)
        {
            OnRequestInvoked = true;
            return base.OnRequestAsync(
                session,
                operationSessionId,
                requestBuilder,
                cancellationToken);
        }

        public override ValueTask<IQueryResult> OnResultAsync(
            ISocketSession session,
            string operationSessionId,
            IQueryResult result,
            CancellationToken cancellationToken = default)
        {
            OnResultCount++;
            OnResultInvoked = true;
            return base.OnResultAsync(session, operationSessionId, result, cancellationToken);
        }

        public override ValueTask OnCompleteAsync(
            ISocketSession session,
            string operationSessionId,
            CancellationToken cancellationToken = default)
        {
            OnCompleteInvoked = true;
            return base.OnCompleteAsync(session, operationSessionId, cancellationToken);
        }

        public override ValueTask OnCloseAsync(
            ISocketSession session,
            CancellationToken cancellationToken = default)
        {
            OnCloseInvoked = true;
            return base.OnCloseAsync(session, cancellationToken);
        }
    }
}
