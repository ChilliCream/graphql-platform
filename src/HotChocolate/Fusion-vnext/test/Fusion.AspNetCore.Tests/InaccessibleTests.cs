using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class InaccessibleTests : FusionTestBase
{
    [Fact]
    public async Task Inaccessible_Fields_Cannot_Be_Queried_Via_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "Author") {
                fields {
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
    public async Task Inaccessible_Fields_Can_Be_Used_As_Requirements()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              bookById(id: 1) {
                author {
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
    public async Task Inaccessible_Fields_Cannot_Be_Queried()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              bookById(id: 1) {
                author {
                  id
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

    public static class InaccessibleField
    {
        public static class SourceSchema1
        {
            public record Book(int Id, string Title, Author Author);

            [EntityKey("id")]
            public record Author([property: Inaccessible] int Id);

            public class Query
            {
                private readonly OrderedDictionary<int, Book> _books =
                    new()
                    {
                        [1] = new Book(1, "C# in Depth", new Author(1)),
                        [2] = new Book(2, "The Lord of the Rings", new Author(2)),
                        [3] = new Book(3, "The Hobbit", new Author(2)),
                        [4] = new Book(4, "The Silmarillion", new Author(2))
                    };

                [Lookup]
                public Book GetBookById(int id)
                    => _books[id];

                public IEnumerable<Book> GetBooks()
                    => _books.Values;
            }
        }

        public static class SourceSchema2
        {
            public record Author(int Id, string Name);

            public class Query
            {
                private readonly OrderedDictionary<int, Author> _authors = new()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

                [Internal]
                [Lookup]
                public Author GetAuthorById(int id)
                    => _authors[id];
            }
        }
    }
}
