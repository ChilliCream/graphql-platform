using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions.Apollo;

public class LargeMessageTests : SubscriptionTestBase
{
    public LargeMessageTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public Task Send_Start_ReceiveDataOnMutation_Large_Message()
    {
        Snapshot.FullName();

        return TryTest(async ct =>
        {
            // arrange
            using TestServer testServer = CreateStarWarsServer();
            WebSocketClient client = CreateWebSocketClient(testServer);
            WebSocket webSocket = await ConnectToServerAsync(client, ct);

            DocumentNode document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request, true);

            // assert
            await webSocket.SendEmptyMessageAsync(ct);
            await testServer.SendPostRequestAsync(new ClientQueryRequest
            {
                Query = @"
                    mutation {
                        createReview(episode:NEW_HOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }
                "
            });

            var message = await WaitForMessage(webSocket, "data", TimeSpan.FromSeconds(15), ct);
            Assert.NotNull(message);
            message.MatchSnapshot();
        });
    }
}
