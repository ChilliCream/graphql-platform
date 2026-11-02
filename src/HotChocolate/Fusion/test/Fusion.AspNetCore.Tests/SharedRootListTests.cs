using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SharedRootListTests : FusionTestBase
{
    [Fact]
    public async Task Shared_Root_List_Composes_Fields_From_Three_Subgraphs()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SchemaA.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SchemaB.Query>());

        using var server3 = CreateSourceSchema(
            "C",
            b => b.AddQueryType<SchemaC.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ products { id a b c } }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "1",
                    "a": "a",
                    "b": "b",
                    "c": "c"
                  }
                ]
              }
            }
            """);
    }

    public static class SchemaA
    {
        public class Query
        {
            [Shareable]
            public Product[] Products => [new Product()];
        }

        public class Product
        {
            [Shareable]
            public string Id => "1";

            public string A => "a";
        }
    }

    public static class SchemaB
    {
        public class Query
        {
            [Shareable]
            public Product[] Products => [new Product()];
        }

        public class Product
        {
            [Shareable]
            public string Id => "1";

            public string B => "b";
        }
    }

    public static class SchemaC
    {
        public class Query
        {
            [Shareable]
            public Product[] Products => [new Product()];
        }

        public class Product
        {
            [Shareable]
            public string Id => "1";

            public string C => "c";
        }
    }
}
