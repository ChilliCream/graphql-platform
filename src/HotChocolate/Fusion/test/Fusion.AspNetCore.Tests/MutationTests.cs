using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class MutationTests : FusionTestBase
{
    [Fact]
    public async Task Single_Mutation()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddMutationType<SourceSchema1.Mutation>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            mutation {
              a: createBook(input: { title: "Book1" }) {
                book {
                  id
                  author
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Multiple_Mutation()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddMutationType<SourceSchema1.Mutation>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            mutation {
              a: createBook(input: { title: "Book1" }) {
                book {
                  id
                  author
                }
              }
              b: createBook(input: { title: "Book2" }) {
                book {
                  id
                  title
                  author
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Mutation_Root_With_Batched_Node_Lookups_Executes_Followups()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            type Mutation {
              createReview(input: CreateReviewInput!): Review
            }

            input CreateReviewInput {
              id: ID!
            }

            interface Node {
              id: ID!
            }

            type Review implements Node {
              id: ID!
              body: String
              author: Author
              product: Product
            }

            type Author implements Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Author implements Node {
              id: ID!
              name: String
            }

            type Product implements Node {
              id: ID!
              name: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            mutation createReview($input: CreateReviewInput!) {
              createReview(input: $input) {
                id
                body
                author { id name }
                product { id name }
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = new Dictionary<string, object?>
                {
                    // Review:1
                    ["id"] = "UmV2aWV3OjE="
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public string Foo => "Foo";
        }

        public class Mutation
        {
            private int _nextId;

            public CreateBookPayload CreateBook(CreateBookInput input)
            {
                var id = Interlocked.Increment(ref _nextId);
                return new CreateBookPayload(new Book(id, input.Title));
            }
        }

        public record CreateBookInput(string Title);

        public record CreateBookPayload(Book Book);

        [EntityKey("id")]
        public record Book(int Id, string Title);
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Internal, Lookup]
            public Book? GetBookById(int id) => new(id, "Abc");
        }

        public record Book(int Id, string Author);
    }
}
