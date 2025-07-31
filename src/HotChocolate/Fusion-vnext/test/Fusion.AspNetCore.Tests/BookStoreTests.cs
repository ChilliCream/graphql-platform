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
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              bookById(id: 1) {
                id
                title
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
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

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result1 = await client.PostAsync(
            """
            {
              bookById(id: 1) {
                id
                title
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        using (var response = await result1.ReadAsResultAsync())
        {
            response.MatchSnapshot();
        }

        using var result2 = await client.PostAsync(
            """
            {
              bookById(id: 1) {
                id
                title
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using (var response = await result2.ReadAsResultAsync())
        {
            response.MatchSnapshot();
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

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              bookById(id: 1) {
                title
                author {
                  name
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
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
            ("B", server2),
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

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
        response.MatchSnapshot();
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
            ("B", server2),
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              books {
                nodes {
                  idAndTitle
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
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
                _authors = new() { [1] = new Author(1, "Jon Skeet"), [2] = new Author(2, "JRR Tolkien") };

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
