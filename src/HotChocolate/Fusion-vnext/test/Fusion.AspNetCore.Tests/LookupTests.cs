using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class LookupTests : FusionTestBase
{
    [Fact]
    public async Task Fetch_From_Nested_Internal_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<NestedLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<NestedLookups.SourceSchema2.Query>());

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
              books {
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
    public async Task Fetch_OneOf_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<OneOfLookups.SourceSchema2.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<OneOfLookups.SourceSchema3.Query>());

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
              topAuthor {
                id
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

    public static class NestedLookups
    {
        public static class SourceSchema1
        {
            public record Book(int Id, string Title, [property: Shareable] Author Author);

            [EntityKey("id")]
            public record Author(int Id);

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

                public IEnumerable<Book> GetBooks()
                    => _books.Values;
            }
        }

        public static class SourceSchema2
        {
            public record Author(int Id, string Name);

            public class Query
            {
                [Internal]
                public InternalLookups Lookups { get; } = new();
            }

            [Internal]
            public class InternalLookups
            {
                private readonly OrderedDictionary<int, Author> _authors = new()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

                [Lookup]
                public Author GetAuthorById(int id)
                    => _authors[id];
            }

            public record Book(int Id, [property: Shareable] Author Author)
            {
                public string IdAndTitle([Require] string title)
                    => $"{Id} - {title}";
            }
        }
    }

    public static class OneOfLookups
    {
        public static class SourceSchema1
        {
            public record Book(int Id, string Title, [property: Shareable] Author Author);

            [EntityKey("id")]
            public record Author(int Id);

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

                public IEnumerable<Book> GetBooks()
                    => _books.Values;
            }
        }

        public static class SourceSchema2
        {
            public record Author(int Id, string Name);

            public class Query
            {
                [Internal]
                public InternalLookups Lookups { get; } = new();
            }

            [Internal]
            public class InternalLookups
            {
                private readonly OrderedDictionary<int, Author> _authors = new()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

                [Lookup, Internal]
                public Author GetAuthor([Is("{ id } | { name }")] AuthorByInput by)
                {
                    if (by.Id is not null)
                    {
                        return _authors[int.Parse(by.Id)];
                    }

                    return _authors.Values.First(a => a.Name == by.Name);
                }
            }

            public record Book(int Id, [property: Shareable] Author Author)
            {
                public string IdAndTitle([Require] string title)
                    => $"{Id} - {title}";
            }

            [OneOf]
            public record AuthorByInput(string? Id, string? Name);
        }

        public static class SourceSchema3
        {
            public class Query
            {
                public Author GetTopAuthor()
                    => new("Jon Skeet");
            }

            public record Author([property: Shareable] string Name);
        }
    }
}
