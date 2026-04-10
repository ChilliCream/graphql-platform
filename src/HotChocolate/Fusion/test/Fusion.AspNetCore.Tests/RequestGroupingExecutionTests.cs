using System.Collections.Concurrent;
using System.Net;
using System.Text;
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

        AssertAllRequestsAreVariableBatches(bInteractions, expectedVariablesCount: 2);
        AssertAllRequestsAreVariableBatches(cInteractions, expectedVariablesCount: 2);
    }

    [Fact]
    public async Task Execute_With_RequestGrouping_Enabled_Does_Not_Deadlock_Across_Depths()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "a",
            builder => builder.AddQueryType<DeadlockSourceSchemaA.Query>());

        using var serverC = CreateSourceSchema(
            "c",
            builder => builder.AddQueryType<DeadlockSourceSchemaC.Query>());

        using var serverD = CreateSourceSchema(
            "d",
            builder => builder.AddQueryType<DeadlockSourceSchemaD.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", serverA),
                ("c", serverC),
                ("d", serverD)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyPlannerOptions(options => options.EnableRequestGrouping = true));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            """
            {
              users {
                id
                reviews {
                  id
                  author {
                    id
                    name
                  }
                }
              }
              topProducts {
                id
                reviews {
                  id
                  author {
                    id
                    name
                  }
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql")).WaitAsync(TimeSpan.FromSeconds(5));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.Equal(JsonValueKind.Object, response.Data.ValueKind);

        var users = response.Data.GetProperty("users");
        Assert.Equal(JsonValueKind.Array, users.ValueKind);
        Assert.True(users.GetArrayLength() > 0);

        var firstUser = users[0];
        var firstUserReview = firstUser.GetProperty("reviews")[0];
        var firstAuthor = firstUserReview.GetProperty("author");
        var firstAuthorId = firstAuthor.GetProperty("id").GetInt32();
        Assert.Equal($"User {firstAuthorId + 100}", firstAuthor.GetProperty("name").GetString());

        var topProducts = response.Data.GetProperty("topProducts");
        Assert.Equal(JsonValueKind.Array, topProducts.ValueKind);
        Assert.True(topProducts.GetArrayLength() > 0);
        Assert.Equal(JsonValueKind.Array, topProducts[0].GetProperty("reviews").ValueKind);
    }

    [Fact]
    public async Task Execute_With_RequestGrouping_Enabled_When_Subgraph_Rejects_Request_Without_Indexes()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "a",
            builder => builder.AddQueryType<SourceSchemaA.Query>());

        using var serverB = CreateSourceSchema(
            "b",
            """
            schema {
              query: Query
            }

            type Product @key(fields: "id") {
              id: Int!
              rating: Int!
            }

            type Query {
              productById(id: Int!): Product @lookup @internal
            }
            """,
            httpClient: new HttpClient(new RejectedBeforeExecutionHandler()));

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

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
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
        Assert.Equal(JsonValueKind.Array, response.Errors.ValueKind);
        Assert.True(response.Errors.GetArrayLength() > 0);

        var bInteractions = AssertSchemaInteractions(gateway.Interactions, "b");
        Assert.Contains(
            bInteractions.Values.SelectMany(interaction => interaction.Results),
            result => result.Contains("Cannot query field", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Execute_When_Subgraph_Rejects_Variable_Batch_Without_VariableIndex()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "a",
            builder => builder.AddQueryType<SourceSchemaA.Query>());

        using var serverB = CreateSourceSchema(
            "b",
            """
            schema {
              query: Query
            }

            type Product @key(fields: "id") {
              id: Int!
              rating: Int!
            }

            type Query {
              productById(id: Int!): Product @lookup @internal
            }
            """,
            httpClient: new HttpClient(new RejectedBeforeExecutionHandler()));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", serverA),
                ("b", serverB)
            ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            """
            {
              products {
                id
                rating
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.Equal(JsonValueKind.Array, response.Errors.ValueKind);
        Assert.True(response.Errors.GetArrayLength() > 0);

        var bInteractions = AssertSchemaInteractions(gateway.Interactions, "b");
        Assert.Contains(
            bInteractions.Values.SelectMany(interaction => interaction.Results),
            result => result.Contains("Cannot query field", StringComparison.Ordinal));
        AssertAllRequestsAreVariableBatches(bInteractions, expectedVariablesCount: 2);
    }

    private static ConcurrentDictionary<int, SourceSchemaInteraction> AssertSchemaInteractions(
        ConcurrentDictionary<string, ConcurrentDictionary<int, SourceSchemaInteraction>> interactions,
        string schemaName)
    {
        Assert.True(interactions.TryGetValue(schemaName, out var schemaInteractions));
        Assert.NotNull(schemaInteractions);
        // Equivalent operations are merged into a single OperationBatchExecutionNode
        // that uses variable batching, so there is one interaction per schema.
        Assert.Single(schemaInteractions);
        return schemaInteractions;
    }

    private static void AssertAllRequestsAreVariableBatches(
        ConcurrentDictionary<int, SourceSchemaInteraction> interactions,
        int expectedVariablesCount)
    {
        foreach (var interaction in interactions.Values)
        {
            Assert.NotNull(interaction.Request);
            var request = interaction.Request!;
            request.Body.Position = 0;

            using var body = JsonDocument.Parse(request.Body);
            Assert.Equal(JsonValueKind.Object, body.RootElement.ValueKind);
            Assert.True(body.RootElement.TryGetProperty("variables", out var variables));
            Assert.Equal(JsonValueKind.Array, variables.ValueKind);
            Assert.Equal(expectedVariablesCount, variables.GetArrayLength());
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

            public IReadOnlyList<Product> GetProducts() => [new(1), new(2)];
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

    public static class DeadlockSourceSchemaA
    {
        [EntityKey("id")]
        public record User(int Id, string Name);

        public sealed class Query
        {
            public IReadOnlyList<User> GetUsers() => [new(1, "User 1")];

            [Lookup]
            [Internal]
            public User GetUserById(int id) => new(id, $"User {id + 100}");
        }
    }

    public static class DeadlockSourceSchemaC
    {
        [EntityKey("id")]
        public record Product(int Id);

        public sealed class Query
        {
            public IReadOnlyList<Product> GetTopProducts() => [new(10)];

            [Lookup]
            [Internal]
            public Product GetProductById(int id) => new(id);
        }
    }

    public static class DeadlockSourceSchemaD
    {
        [EntityKey("id")]
        public record User(int Id)
        {
            public IReadOnlyList<Review> Reviews => [new(Id * 10, new User(Id + 1))];
        }

        [EntityKey("id")]
        public record Product(int Id)
        {
            public IReadOnlyList<Review> Reviews => [new(Id * 10, new User(Id + 2))];
        }

        public record Review(int Id, User Author);

        public sealed class Query
        {
            [Lookup]
            [Internal]
            public User GetUserById(int id) => new(id);

            [Lookup]
            [Internal]
            public Product GetProductById(int id) => new(id);
        }
    }

    private sealed class RejectedBeforeExecutionHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"errors":[{"message":"Cannot query field \"rating\" on type \"Product\"."}],"data":null}
                    """,
                    Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
