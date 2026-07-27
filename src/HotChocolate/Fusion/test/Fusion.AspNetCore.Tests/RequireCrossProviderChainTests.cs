using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class RequireCrossProviderChainTests : FusionTestBase
{
    [Fact]
    public async Task Feed_ByExpert_Chains_Through_ByNovice_And_Author_CrossProvider()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<ByExpertChainCrossProvider.SchemaA.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<ByExpertChainCrossProvider.SchemaB.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ feed { byExpert } }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "feed": [
                  {
                    "byExpert": true
                  }
                ]
              }
            }
            """);
    }

    public static class ByExpertChainCrossProvider
    {
        public static class SchemaA
        {
            public class Query
            {
                public IEnumerable<Post> GetFeed() => [new Post(1)];

                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);

                [Lookup]
                [Internal]
                public Author? GetAuthorById([ID] int id) => new Author(id, id == 10 ? 5 : 0);
            }

            [EntityKey("id")]
            public record Post([property: ID] int Id)
            {
                public bool GetByExpert([Require("byNovice")] bool byNovice) => byNovice;
            }

            public record Author([property: ID] int Id, int YearsOfExperience);
        }

        public static class SchemaB
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public Author GetAuthor() => new(10);

                public bool GetByNovice(
                    [Require("author.yearsOfExperience")] int yearsOfExperience)
                    => yearsOfExperience >= 3;
            }

            [EntityKey("id")]
            public record Author([property: ID] int Id);
        }
    }
}
