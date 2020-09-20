using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class LargeMessageTests
        : SubscriptionTestBase
    {
        public LargeMessageTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact(Skip = "TODO: This test is flaky")]
        public Task Send_Start_ReceiveDataOnMutation_Large_Message()
        {
            Snapshot.FullName();

            return TryTest(async () =>
            {
                // arrange
                using TestServer testServer = CreateStarWarsServer();
                WebSocketClient client = CreateWebSocketClient(testServer);
                WebSocket webSocket = await ConnectToServerAsync(client);

                DocumentNode document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");

                var request = new GraphQLRequest(document);

                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscriptionStartAsync(
                    subscriptionId, request, true);

                // assert
                await webSocket.SendEmptyMessageAsync();

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

                IReadOnlyDictionary<string, object> message =
                    await WaitForMessage(
                        webSocket,
                        MessageTypes.Subscription.Data,
                        TimeSpan.FromSeconds(15));

                Assert.NotNull(message);
                message.MatchSnapshot();
            });
        }
    }
}
