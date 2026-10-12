using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class MergedRequirementTests : FusionTestBase
{
    [Fact]
    public async Task Requirements_Should_ReachEachField_When_MergedLookupSelectsRequirementFieldOnce()
    {
        // arrange
        var server1 = CreateSourceSchema("a", b => b.AddQueryType<SchemaA.Query>());
        var server2 = CreateSourceSchema("b", b => b.AddQueryType<SchemaB.Query>());
        var server3 = CreateSourceSchema("c", b => b.AddQueryType<SchemaC.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              products {
                id
                scaled
                labelOne
                labelTwo
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    public static class SchemaA
    {
        public class Query
        {
            public IEnumerable<Product> GetProducts() => [new(1, 3), new(2, 7)];

            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => id switch
            {
                1 => new Product(1, 3),
                2 => new Product(2, 7),
                _ => null
            };
        }

        public record Product([property: ID] int Id, int Base);
    }

    public static class SchemaB
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new Product(id);
        }

        public record Product([property: ID] int Id)
        {
            public int GetScaled([Require("base")] int @base) => @base * 10;
        }
    }

    public static class SchemaC
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new Product(id);
        }

        public record Product([property: ID] int Id)
        {
            public string GetLabelOne([Require("scaled")] int scaled) => $"one:{scaled}";

            public string GetLabelTwo([Require("scaled")] int scaled) => $"two:{scaled}";
        }
    }
}
