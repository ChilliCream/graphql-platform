using HotChocolate.Execution;

namespace HotChocolate.Types;

public class GeneratorTestServerTests
{
    [Fact]
    public async Task Subscription_With_Subscribe_With_Delivers_Message_From_Stream()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace Demo;

            [QueryType]
            public static partial class Query
            {
                public static int Product => 1;
            }

            [SubscriptionType]
            public static partial class Subscription
            {
                [Subscribe(With = nameof(SubscribeToOnProductAdded))]
                public static Task<int> OnProductAdded([EventMessage] int productId)
                    => Task.FromResult(productId);

                private static async IAsyncEnumerable<int> SubscribeToOnProductAdded(int categoryId)
                {
                    await Task.Yield();
                    yield return categoryId;
                }
            }
            """;

        var executor = await GeneratorTestServer.CreateExecutorAsync(
            source,
            disableDefaultSecurity: false);

        // act
        await using var subscriptionResult = await executor.ExecuteAsync(
            "subscription { onProductAdded(categoryId: 42) }");

        // assert
        var stream = subscriptionResult.ExpectResponseStream();
        await foreach (var result in stream.ReadResultsAsync())
        {
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "onProductAdded": 42
                  }
                }
                """);
            break;
        }
    }
}
