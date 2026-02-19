using System.Collections.Concurrent;
using System.Text.Json;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class RequestGroupingExecutionTests : FusionTestBase
{
    [Fact]
    public async Task Execute_With_RequestGrouping_Enabled_Coalesces_Lookup_Requests()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "a",
            builder => builder.AddQueryType<SourceSchemaA.Query>());

        using var serverB = CreateSourceSchema(
            "b",
            builder => builder.AddQueryType<SourceSchemaB.Query>());

        using var serverC = CreateSourceSchema(
            "c",
            builder => builder.AddQueryType<SourceSchemaC.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", serverA),
                ("b", serverB),
                ("c", serverC)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyPlannerOptions(options => options.EnableRequestGrouping = true));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              first {
                id
                rating
                deliveryEstimate
              }
              second {
                id
                rating
                deliveryEstimate
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.Equal(JsonValueKind.Object, response.Data.ValueKind);

        var first = response.Data.GetProperty("first");
        Assert.Equal(1, first.GetProperty("id").GetInt32());
        Assert.Equal(5, first.GetProperty("rating").GetInt32());
        Assert.Equal(2, first.GetProperty("deliveryEstimate").GetInt32());

        var second = response.Data.GetProperty("second");
        Assert.Equal(2, second.GetProperty("id").GetInt32());
        Assert.Equal(4, second.GetProperty("rating").GetInt32());
        Assert.Equal(3, second.GetProperty("deliveryEstimate").GetInt32());

        var bInteractions = AssertSchemaInteractions(gateway.Interactions, "b");
        var cInteractions = AssertSchemaInteractions(gateway.Interactions, "c");

        AssertAllRequestsAreOperationBatches(bInteractions, expectedBatchSize: 2);
        AssertAllRequestsAreOperationBatches(cInteractions, expectedBatchSize: 2);
    }

    private static ConcurrentDictionary<int, SourceSchemaInteraction> AssertSchemaInteractions(
        ConcurrentDictionary<string, ConcurrentDictionary<int, SourceSchemaInteraction>> interactions,
        string schemaName)
    {
        Assert.True(interactions.TryGetValue(schemaName, out var schemaInteractions));
        Assert.NotNull(schemaInteractions);
        Assert.Equal(2, schemaInteractions.Count);
        return schemaInteractions;
    }

    private static void AssertAllRequestsAreOperationBatches(
        ConcurrentDictionary<int, SourceSchemaInteraction> interactions,
        int expectedBatchSize)
    {
        foreach (var interaction in interactions.Values)
        {
            Assert.NotNull(interaction.Request);
            var request = interaction.Request!;
            request.Body.Position = 0;

            using var body = JsonDocument.Parse(request.Body);
            Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
            Assert.Equal(expectedBatchSize, body.RootElement.GetArrayLength());
        }
    }

    public static class SourceSchemaA
    {
        [EntityKey("id")]
        public record Product(int Id);

        public sealed class Query
        {
            public Product GetFirst() => new(1);

            public Product GetSecond() => new(2);
        }
    }

    public static class SourceSchemaB
    {
        private static readonly Dictionary<int, Product> s_products = new()
        {
            [1] = new Product(1, 5),
            [2] = new Product(2, 4)
        };

        [EntityKey("id")]
        public record Product(int Id, int Rating);

        public sealed class Query
        {
            [Lookup]
            [Internal]
            public Product GetProductById(int id) => s_products[id];
        }
    }

    public static class SourceSchemaC
    {
        private static readonly Dictionary<int, Product> s_products = new()
        {
            [1] = new Product(1, 2),
            [2] = new Product(2, 3)
        };

        [EntityKey("id")]
        public record Product(int Id, int DeliveryEstimate);

        public sealed class Query
        {
            [Lookup]
            [Internal]
            public Product GetProductById(int id) => s_products[id];
        }
    }
}
