using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.AspNetCore.Subscriptions.GraphQLOverWebSocket;

public class WebSocketClientProtocolTests : SubscriptionTestBase
{
    public WebSocketClientProtocolTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task Send_Connect_Accept()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient webSocketClient = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            await SocketClient.ConnectAsync(webSocket, ct);

            // assert
            // no error
        });

    [Fact]
    public Task Subscribe_ReceiveDataOnMutation()
    {
        Snapshot.FullName();

        return TryTest(async ct =>
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

            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient webSocketClient = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            SocketClient client = await SocketClient.ConnectAsync(webSocket, ct);
            string? result = null;

            // act
            // ... subscribe
            using SocketResult socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // ... trigger event
            await testServer.SendPostRequestAsync(mutationRequest);

            // receive event result on the stream
            await foreach (OperationResult operationResult in
                socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                result = operationResult.Data.ToString();
                operationResult.Dispose();
                break;
            }

            // assert
            result.MatchSnapshot();
        });
    }

    [Fact]
    public Task Subscribe_Disconnect()
    {
        Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient webSocketClient = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            SocketClient client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            // ... subscribe
            using SocketResult socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // ... disconnect
            webSocket.Abort();

            // assert
            // ... try iterate
            await foreach (OperationResult unused in
                socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                Assert.True(false, "Stream should have been aborted");
            }
        });
    }

    [Fact]
    public Task Send_Subscribe_SyntaxError()
    {
        Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { 123 } }");

            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient webSocketClient = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            SocketClient client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            SocketResult socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // assert
            await foreach (OperationResult result in
                socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                Assert.Null(result.Data);
                Assert.NotNull(result.Errors);
                Assert.Null(result.Extensions);
                result.Errors.MatchSnapshot();
            }
        });
    }

    [Fact]
    public Task Send_Subscribe_ValidationError()
    {
        Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { ____ } }");

            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient webSocketClient = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            SocketClient client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            SocketResult socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

            // assert
            await foreach (OperationResult result in
                socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                Assert.Null(result.Data);
                Assert.NotNull(result.Errors);
                Assert.Null(result.Extensions);
                result.Errors.MatchSnapshot();
            }
        });
    }

    [Fact]
    public Task Send_Connect_With_Auth_Accept()
        => TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using TestServer testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            WebSocketClient webSocketClient = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            await SocketClient.ConnectAsync(webSocket, new Auth { Token = "abc" }, ct);

            // assert
            // no error
        });

    [Fact]
    public Task Send_Connect_With_Auth_Reject()
        => TryTest(async ct =>
        {
            // arrange
            var interceptor = new AuthInterceptor();
            using TestServer testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            WebSocketClient webSocketClient = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

            // act
            async Task Connect() => await SocketClient.ConnectAsync(webSocket, ct);

            // assert
            SocketClosedException error = await Assert.ThrowsAsync<SocketClosedException>(Connect);
            Assert.Equal(4401, (int)error.Reason);
        });

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
    }

    private sealed class Auth
    {
        public string? Token { get; set; }
    }
}
