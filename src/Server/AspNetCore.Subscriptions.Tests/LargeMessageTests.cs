using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Tests.Utilities;
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

        [Fact(Skip = "Fix this one.")]
        public async Task Send_Start_ReceiveDataOnMutation_Large_Message()
        {
            using (TestServer testServer = CreateStarWarsServer())
            {
                // arrange
                WebSocketClient client = CreateWebSocketClient(testServer);
                WebSocket webSocket = await ConnectToServerAsync(client);

                var document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEWHOPE) { stars } }");

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
                        createReview(episode:NEWHOPE review: {
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
            }
        }
    }
}
