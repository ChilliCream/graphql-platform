using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class IntrospectionTests : FusionTestBase
{
    [Fact]
    public async Task Fetch_Schema_Types_Name()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            {
              __schema {
                types {
                  name
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
    public async Task Fetch_Specific_Type()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            {
              __type(name: "String") {
                name
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
    public async Task Typename_On_Query()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            {
              __typename
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Typename_On_Query_Skip_True()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            query ($s: Boolean! = true) {
              __typename @skip(if: $s)
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Typename_On_Query_Skip_False()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            query ($s: Boolean! = false) {
              __typename @skip(if: $s)
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Typename_On_Query_With_Alias()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            {
              a: __typename
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Typename_On_Object()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            {
              books {
                nodes {
                  __typename
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
    public async Task Typename_On_Object_With_Alias()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

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

        var request = new Transport.OperationRequest(
            """
            {
              books {
                nodes {
                  a: __typename
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

    [Theory]
    [InlineData("SchemaCapabilitiesQuery")]
    [InlineData("InputValueCapabilitiesQuery")]
    [InlineData("DirectiveCapabilitiesQuery")]
    [InlineData("TypeCapabilitiesQuery")]
    [InlineData("IntrospectionQuery")]
    public async Task IntrospectionQueries(string fileName)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            schema @test(arg: "value") {
              query: Query
              mutation: Mutation
              subscription: Subscription
            }

            type Query @test(arg: "value") {
              posts(filter: PostsFilter, first: Int! = 5 @test(arg: "value"), hidden: Boolean): [Post]
              userCreation: UserCreation
              votables: [Votable]!
              postById(postId: ID! @is(field: "id")): Post @lookup
              node(id: ID!): Node @lookup
            }

            type Mutation @test(arg: "value") {
              postReview(input: PostReviewInput): Review @test(arg: "value")
            }

            type Subscription @test(arg: "value") {
              onNewReview: Review
            }

            input PostsFilter @test(arg: "value") {
              scalar: String = "test" @test(arg: "value")
            }

            input PostReviewInput @oneOf {
              scalar: String
              pros: [PostReviewPro]
            }

            input PostReviewPro {
              scalar: Int!
            }

            union UserCreation @test(arg: "value") = Post | Review

            interface Votable implements Node {
              id: ID!
              # voteCount: StarRating!
            }

            interface Node @test(arg: "value") {
              id: ID!
            }

            type Post implements Votable @key(fields: "id") {
              id: ID!
              # voteCount: StarRating!
              postKind: PostKind @shareable
              location: String @inaccessible
            }

            type Review implements Votable @test(arg: "value") {
              id: ID!
              # voteCount: StarRating!
            }

            enum PostKind @test(arg: "value") {
              STORY @test(arg: "value")
              PHOTO
            }

            # scalar StarRating @specifiedBy(url: "https://tools.ietf.org/html/rfc4122") @test(arg: "value")

            directive @oneOf on INPUT_OBJECT

            directive @test(arg: String! = "default") repeatable on
              | QUERY
              | MUTATION
              | SUBSCRIPTION
              | FIELD
              | FRAGMENT_DEFINITION
              | FRAGMENT_SPREAD
              | INLINE_FRAGMENT
              | VARIABLE_DEFINITION
              | SCHEMA
              | SCALAR
              | OBJECT
              | FIELD_DEFINITION
              | ARGUMENT_DEFINITION
              | INTERFACE
              | UNION
              | ENUM
              | ENUM_VALUE
              | INPUT_OBJECT
              | INPUT_FIELD_DEFINITION
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new Transport.OperationRequest(FileResource.Open(fileName + ".graphql"));

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: fileName);
    }

    [Fact]
    public async Task Download_Schema()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = gateway.CreateClient();

        using var result = await client.GetAsync("http://localhost:5000/graphql/schema");
        var sdl = await result.Content.ReadAsStringAsync();

        // assert
        sdl.MatchSnapshot(extension: ".graphql");
    }

    public static class SourceSchema1
    {
        public record Book(int Id, string Title, Author Author);

        public record Author(int Id);

        public class Query
        {
            private readonly OrderedDictionary<int, Book> _books =
                new OrderedDictionary<int, Book>()
                {
                    [1] = new Book(1, "C# in Depth", new Author(1)),
                    [2] = new Book(2, "The Lord of the Rings", new Author(2)),
                    [3] = new Book(3, "The Hobbit", new Author(2)),
                    [4] = new Book(4, "The Silmarillion", new Author(2))
                };

            [Lookup]
            public Book GetBookById(int id)
                => _books[id];

            [UsePaging]
            public IEnumerable<Book> GetBooks()
                => _books.Values;
        }
    }

    public static class SourceSchema2
    {
        public record Author(int Id, string Name)
        {
            public IEnumerable<Book> GetBooks()
            {
                if (Id == 1)
                {
                    yield return new Book(1, this);
                }
                else
                {
                    yield return new Book(2, this);
                    yield return new Book(3, this);
                    yield return new Book(4, this);
                }
            }
        }

        public class Query
        {
            private readonly OrderedDictionary<int, Author> _authors =
                new OrderedDictionary<int, Author>()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

            [Internal]
            [Lookup]
            public Author GetAuthorById(int id)
                => _authors[id];

            [UsePaging]
            public IEnumerable<Author> GetAuthors()
                => _authors.Values;
        }

        public record Book(int Id, Author Author);
    }
}
