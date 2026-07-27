using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class RequireCrossProviderTests : FusionTestBase
{
    [Fact]
    public async Task Feed_ByNovice_Requires_Author_YearsOfExperience_CrossProvider()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CircularCrossProvider.SchemaA.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<CircularCrossProvider.SchemaB.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ feed { byNovice } }",
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
                    "byNovice": true
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Feed_NeedsFlag_Requires_Flag_SameEntity_CrossProvider()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SameEntityCrossProvider.SchemaA.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SameEntityCrossProvider.SchemaB.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ feed { needsFlag } }",
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
                    "needsFlag": true
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Feed_Author_Requires_Comments_AuthorId_List_CrossProvider()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<ListCrossProvider.SchemaC.Query>());

        using var server2 = CreateSourceSchema(
            "d",
            b => b.AddQueryType<ListCrossProvider.SchemaD.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("c", server1),
            ("d", server2)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ feed { author { id } } }",
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
                    "author": {
                      "id": "10_11"
                    }
                  }
                ]
              }
            }
            """);
    }

    // Two-hop object require crossing Post.author into Author.
    public static class CircularCrossProvider
    {
        public static class SchemaA
        {
            public class Query
            {
                public IEnumerable<Post> GetFeed() => [new Post(1)];

                [Lookup]
                [Internal]
                public Author? GetAuthorById([ID] int id)
                    => new Author(id, id == 10 ? 5 : 0);
            }

            [EntityKey("id")]
            public record Post([property: ID] int Id);

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

    // Same-entity scalar require crossing from Post.needsFlag to Post.flag.
    public static class SameEntityCrossProvider
    {
        public static class SchemaA
        {
            public class Query
            {
                public IEnumerable<Post> GetFeed() => [new Post(1)];

                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public bool GetNeedsFlag([Require("flag")] bool flag) => flag;
            }
        }

        public static class SchemaB
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id, true);
            }

            public record Post([property: ID] int Id, bool Flag);
        }
    }

    // List require crossing Post.comments into Comment.authorId.
    public static class ListCrossProvider
    {
        public static class SchemaC
        {
            public class Query
            {
                public IEnumerable<Post> GetFeed() => [new Post(1)];

                [Lookup]
                [Internal]
                public Comment? GetCommentById([ID] int id)
                    => new Comment(id, id == 100 ? 10 : 11, $"body{id}");
            }

            [EntityKey("id")]
            public record Post([property: ID] int Id);

            public record Comment([property: ID] int Id, [property: ID] int AuthorId, string Body);
        }

        public static class SchemaD
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public IEnumerable<Comment> GetComments(int? limit)
                    => [new Comment(100), new Comment(101)];

                public Author GetAuthor(
                    [Require("comments[authorId]")] [ID] IReadOnlyList<int> commentAuthorIds)
                    => new(string.Join("_", commentAuthorIds));
            }

            [EntityKey("id")]
            public record Comment([property: ID] int Id);

            public record Author(string Id);
        }
    }
}
