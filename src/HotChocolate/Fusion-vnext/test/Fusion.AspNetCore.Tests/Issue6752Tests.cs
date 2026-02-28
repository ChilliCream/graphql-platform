using System.Text.Json;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class Issue6752Tests : FusionTestBase
{
    [Fact]
    public async Task Shared_Entity_Transport_Failure_Does_Not_Null_Entire_Entity()
    {
        // arrange
        using var subgraphA = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchemaA.Query>());

        using var subgraphB = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchemaB.Query>(),
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            query {
              productById(id: 1) {
                name
                reviews {
                  body
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.Equal(JsonValueKind.Array, response.Errors.ValueKind);
        Assert.Equal(JsonValueKind.Object, response.Data.ValueKind);
        var product = response.Data.GetProperty("productById");
        Assert.Equal(JsonValueKind.Object, product.ValueKind);

        Assert.Equal("Product 1", product.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, product.GetProperty("reviews").ValueKind);
    }

    public static class SourceSchemaA
    {
        public sealed class Query
        {
            [Shareable]
            [Lookup]
            public Product? GetProductById(int id) => new(id);
        }

        public sealed record Product(int Id)
        {
            public string Name => $"Product {Id}";
        }
    }

    public static class SourceSchemaB
    {
        public sealed class Query
        {
            [Shareable]
            [Lookup]
            public Product? GetProductById(int id) => new(id);
        }

        public sealed record Product(int Id)
        {
            public IReadOnlyList<Review>? Reviews =>
            [
                new("Review 1"),
                new("Review 2")
            ];
        }

        public sealed record Review(string Body);
    }
}
