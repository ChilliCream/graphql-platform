using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.Transport.Sockets.Client;
using Xunit.Abstractions;

namespace HotChocolate.Transport.Sockets.GraphQLOverWebSocket;

public class WebSocketClientBatchTests(TestServerFactory serverFactory, ITestOutputHelper output)
    : SubscriptionTestBase(serverFactory)
{
    [Fact]
    public Task ExecuteBatch_ReceivesAllResults()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer(output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            var batchRequest = new OperationBatchRequest(
            [
                new OperationRequest("{ hero(episode: NEW_HOPE) { name } }"),
                new OperationRequest("{ hero(episode: EMPIRE) { name } }")
            ]);

            // act
            using var socketResult = await client.ExecuteBatchAsync(batchRequest, ct);

            var results = new List<(int? RequestIndex, string? Data)>();
            await foreach (var result in socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                results.Add((result.RequestIndex, result.Data.ToString()));
                result.Dispose();
            }

            // assert
            Assert.Equal(2, results.Count);
            Assert.Equal(0, results[0].RequestIndex);
            Assert.Equal(1, results[1].RequestIndex);
            Assert.NotNull(results[0].Data);
            Assert.NotNull(results[1].Data);
        });

    [Fact]
    public Task ExecuteBatch_SingleRequest_WorksLikeSingleExecute()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer(output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            var batchRequest = new OperationBatchRequest(
            [
                new OperationRequest("{ hero(episode: NEW_HOPE) { name } }")
            ]);

            // act
            using var socketResult = await client.ExecuteBatchAsync(batchRequest, ct);

            var results = new List<(int? RequestIndex, string? Data)>();
            await foreach (var result in socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                results.Add((result.RequestIndex, result.Data.ToString()));
                result.Dispose();
            }

            // assert
            Assert.Single(results);
            Assert.NotNull(results[0].Data);
        });

    [Fact]
    public Task ExecuteBatch_Disconnect()
    {
        return TryTest(async ct =>
        {
            // arrange
            var subscriptionRequest = new OperationBatchRequest(
            [
                new OperationRequest(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }")
            ]);

            using var testServer = CreateStarWarsServer(output: output);
            var webSocketClient = CreateWebSocketClient(testServer);
            using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);
            var client = await SocketClient.ConnectAsync(webSocket, ct);

            // act
            using var socketResult = await client.ExecuteBatchAsync(subscriptionRequest, ct);

            // disconnect
            webSocket.Abort();

            // assert - iterating the aborted stream should complete without results
            await foreach (var unused in socketResult.ReadResultsAsync().WithCancellation(ct))
            {
                Assert.Fail("Stream should have been aborted");
            }
        });
    }
}
