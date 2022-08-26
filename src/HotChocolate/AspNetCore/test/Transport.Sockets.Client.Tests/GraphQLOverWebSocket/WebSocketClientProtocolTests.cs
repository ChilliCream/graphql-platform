#nullable enable

using CookieCrumble;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Transport.Sockets.GraphQLOverWebSocket;

public class WebSocketClientProtocolTests : SubscriptionTestBase
{
    public WebSocketClientProtocolTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Send_Connect_Accept()
    {
        await TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            await SocketClient.ConnectAsync(webSocket, ct);

            // assert
            // no error
        });
    }

    [Fact]
    public async Task Subscribe_ReceiveDataOnMutation()
    {
        var snapshot = new Snapshot();

        await TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var mutationRequest = new ClientQueryRequest
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
            };

            using var testServer = CreateStarWarsServer();
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            var client = await SocketClient.ConnectAsync(webSocket, ct);
            string? result = null;

            // act
            // ... subscribe
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // ... trigger event
            await testServer.SendPostRequestAsync(mutationRequest);

            // receive event result on the stream
            await foreach (var operationResult in
                           socketResult.ReadResultsAsync()
                               .WithCancellation(ct))
            {
                result = operationResult.Data.ToString();
                operationResult.Dispose();
                break;
            }

            // assert
            await snapshot.Add(result)
                .MatchAsync(ct);
        });
    }

    [Fact]
    public async Task Subscribe_Disconnect()
    {
        await TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            using var testServer = CreateStarWarsServer();
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            // ... subscribe
            using var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // ... disconnect
            webSocket.Abort();

            // assert
            // ... try iterate
            await foreach (var unused in socketResult.ReadResultsAsync()
                               .WithCancellation(ct))
            {
                Assert.True(false, "Stream should have been aborted");
            }
        });
    }

    [Fact]
    public async Task Send_Subscribe_SyntaxError()
    {
        var snapshot = new Snapshot();

        await TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { 123 } }");

            using var testServer = CreateStarWarsServer();
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // assert
            await foreach (var result in socketResult.ReadResultsAsync()
                               .WithCancellation(ct))
            {
                Assert.Null(result.Data);
                Assert.NotNull(result.Errors);
                Assert.Null(result.Extensions);
                snapshot.Add(result.Errors);
            }

            await snapshot.MatchAsync(ct);
        });
    }

    [Fact]
    public async Task Send_Subscribe_ValidationError()
    {
        var snapshot = new Snapshot();

        await TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { ____ } }");

            using var testServer = CreateStarWarsServer();
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // assert
            await foreach (var result in socketResult.ReadResultsAsync()
                               .WithCancellation(ct))
            {
                Assert.Null(result.Data);
                Assert.NotNull(result.Errors);
                Assert.Null(result.Extensions);
                snapshot.Add(result.Errors);
            }

            await snapshot.MatchAsync(ct);
        });
    }

    [Fact]
    public async Task Send_Connect_With_Auth_Accept()
    {
        await TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using var testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            await SocketClient.ConnectAsync(webSocket, new Auth { Token = "abc" }, ct);

            // assert
            // no error
        });
    }

    [Fact]
    public async Task Send_Connect_With_Auth_Reject()
    {
        await TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using var testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            async Task Connect() => await SocketClient.ConnectAsync(webSocket, ct);

            // assert
            var error = await Assert.ThrowsAsync<SocketClosedException>(Connect);
            Assert.Equal(4401, (int)error.Reason);
        });
    }

    private class AuthInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            var payload = connectionInitMessage.As<Auth>();

            if (payload?.Token is not null)
                return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);

            return new ValueTask<ConnectionStatus>(ConnectionStatus.Reject());
        }
    }

    private sealed class Auth
    {
        public string? Token { get; set; }
    }
}
