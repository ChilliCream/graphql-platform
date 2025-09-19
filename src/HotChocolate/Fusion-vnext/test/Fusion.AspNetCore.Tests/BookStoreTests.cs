using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class BookStoreTests : FusionTestBase
{
    [Fact]
    public async Task Fetch_Book_From_SourceSchema1()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>());

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
                id
                title
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Book_From_SourceSchema1_With_Settings()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            schemaSettings:
            """
            {
              "sourceSchemas": {
                "a": {
                  "transports": {
                    "http": {
                      "clientName": "a",
                      "url": "http://localhost:5000/graphql"
                    }
                  }
                },
                "b": {
                  "transports": {
                    "http": {
                      "clientName": "b",
                      "url": "http://localhost:5000/graphql"
                    }
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              bookById(id: 1) {
                id
                title
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Book_From_SourceSchema1_Two_Requests()
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

        var request = new OperationRequest(
            """
            {
              bookById(id: 1) {
                id
                title
              }
            }
            """);

        using var result1 = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using (var response = await result1.ReadAsResultAsync())
        {
            MatchSnapshot(gateway, request, response);
        }

        using var result2 = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using (var response = await result2.ReadAsResultAsync())
        {
            MatchSnapshot(gateway, request, response);
        }
    }

    [Fact]
    public async Task Fetch_Book_From_SourceSchema1_And_Author_From_SourceSchema2()
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

        var request = new OperationRequest(
            """
            {
              bookById(id: 1) {
                title
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
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Books_From_SourceSchema1_And_Authors_From_SourceSchema2()
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

        var request = new OperationRequest(
            """
            {
              books {
                nodes {
                  id
                  title
                  author {
                    name
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            """
            {
              books {
                nodes {
                  id
                  title
                  author {
                    name
                  }
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Books_With_Variable_First_And_First_Is_1()
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

        var request = new OperationRequest(
            """
            query GetBooks($first: Int) {
              books(first: $first) {
                nodes {
                  id
                  title
                  author {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["first"] = 1 });

        using var result = await client.PostAsync(
            request,
            uri: new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Books_With_Variable_First_And_First_Omitted()
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

        var request = new OperationRequest(
            """
            query GetBooks($first: Int) {
              books(first: $first) {
                nodes {
                  id
                  title
                  author {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?>());

        using var result = await client.PostAsync(
            request,
            uri: new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Books_With_Variable_First_Last_And_First_1_And_Last_Omitted()
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

        var request = new OperationRequest(
            """
            query GetBooks($first: Int, $last: Int) {
              books(first: $first, last: $last) {
                nodes {
                  id
                  title
                  author {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["first"] = 1 });

        using var result = await client.PostAsync(
            request,
            uri: new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Books_With_Requirements_To_SourceSchema1()
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

        var request = new OperationRequest(
            """
            {
              books {
                nodes {
                  idAndTitle
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Fetch_Books_With_Requirements_To_SourceSchema1_Three_Times()
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

        var request = new OperationRequest(
            """
            {
              books {
                nodes {
                  idAndTitle
                }
              }
            }
            """);

        // act 1
        using (var client = GraphQLHttpClient.Create(gateway.CreateClient()))
        {
            using var result = await client.PostAsync(
                request,
                new Uri("http://localhost:5000/graphql"));

            // assert 1
            using var response = await result.ReadAsResultAsync();
            MatchSnapshot(gateway, request, response);
        }

        // act 2
        using (var client = GraphQLHttpClient.Create(gateway.CreateClient()))
        {
            using var result = await client.PostAsync(
                request,
                new Uri("http://localhost:5000/graphql"));

            // assert 2
            using var response = await result.ReadAsResultAsync();
            MatchSnapshot(gateway, request, response);
        }

        // act 3
        using (var client = GraphQLHttpClient.Create(gateway.CreateClient()))
        {
            using var result = await client.PostAsync(
                request,
                new Uri("http://localhost:5000/graphql"));

            // assert 3
            using var response = await result.ReadAsResultAsync();
            MatchSnapshot(gateway, request, response);
        }
    }

    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    [Theory]
    public async Task Fetch_Books_With_Requirements_To_SourceSchema1_X_Times(int iterations)
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

        for (var i = 0; i < iterations; i++)
        {
            // act
            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                """
                {
                  books {
                    nodes {
                      idAndTitle
                    }
                  }
                }
                """);

            using var result = await client.PostAsync(
                request,
                new Uri("http://localhost:5000/graphql"));

            // assert
            using var response = await result.ReadAsResultAsync();
            MatchSnapshot(gateway, request, response);
        }
    }

    [Fact]
    public async Task Ensure_String_Literals_Can_Be_Empty()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>());

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
              formatTitle(title: "")
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    [Fact]
    public async Task Ensure_String_Variables_Can_Be_Empty()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query ($s: String!) {
              formatTitle(title: $s)
            }
            """,
            variables: new Dictionary<string, object?> { ["s"] = "" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        MatchSnapshot(gateway, request, response);
    }

    public static class SourceSchema1
    {
        public record Book(int Id, string Title, Author Author);

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

            [Lookup]
            public Book GetBookById(int id)
                => _books[id];

            [UsePaging]
            public IEnumerable<Book> GetBooks()
                => _books.Values;

            public string FormatTitle(string title)
                => title;
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
            private readonly OrderedDictionary<int, Author> _authors;
            private readonly OrderedDictionary<int, Book> _books;

            public Query()
            {
                _authors = new()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

                _books = new()
                {
                    [1] = new Book(1, _authors[1]),
                    [2] = new Book(2, _authors[2]),
                    [3] = new Book(3, _authors[2]),
                    [4] = new Book(4, _authors[2])
                };
            }

            [Internal]
            [Lookup]
            public Book GetBookById(int id)
                => _books[id];

            [Internal]
            [Lookup]
            public Author GetAuthorById(int id)
                => _authors[id];

            [UsePaging]
            public IEnumerable<Author> GetAuthors()
                => _authors.Values;
        }

        public record Book(int Id, Author Author)
        {
            public string IdAndTitle([Require] string title)
                => $"{Id} - {title}";
        }
    }
}
