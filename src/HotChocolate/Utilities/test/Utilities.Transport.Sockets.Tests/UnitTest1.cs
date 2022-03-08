using System.Net.WebSockets;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Utilities.Transport.Sockets;
using HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using SubscriptionTestBase = HotChocolate.AspNetCore.Subscriptions.Apollo.SubscriptionTestBase;

namespace HotChocolate.Utilities.Transport;

[Collection("Sockets")]
public class WebSocketProtocolTests : SubscriptionTestBase
{
    public WebSocketProtocolTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task Send_Connect_Accept()
        => TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await client.ConnectAsync(SubscriptionUri, ct);
            var socketSession = new ClientSocketSession(webSocket);

            // act
            await socketSession.InitializeAsync(new InitPayload(), ct);

            // assert

        });

    [Fact]
    public Task Subscribe_ReceiveDataOnMutation()
    {
        SnapshotFullName snapshotName = Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            using WebSocket webSocket = await ConnectToServerAsync(client, ct);
            var socketSession = new ClientSocketSession(webSocket);
            var request = new OperationRequest(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            // act
            await socketSession.InitializeAsync(new InitPayload(), ct);

            Task.Factory.StartNew(
                async () =>
                {
                    await Task.Delay(1000);

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
                });

            await foreach (var result in socketSession.ExecuteAsync(request).WithCancellation(ct))
            {

            }



            // assert

        });
    }

    public class InitPayload
    {

    }
}
