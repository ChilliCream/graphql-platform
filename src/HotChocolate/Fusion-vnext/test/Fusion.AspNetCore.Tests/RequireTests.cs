using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class RequireTests : FusionTestBase
{
    [Fact]
    public async Task Require_Object_In_A_List()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<BookCatalog.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<BookInventory.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<BookShipping.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // assert
        using var result = await client.PostAsync(
            """
            {
                books {
                  nodes {
                    title
                    estimatedDelivery
                  }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    public static class BookCatalog
    {
        private static readonly Dictionary<int, Book> s_books = new()
        {
            { 1, new Book { Id = 1, Title = "The Great Gatsby", Author = new Author { Id = 1 } } },
            { 2, new Book { Id = 2, Title = "1984", Author = new Author { Id = 2 } } },
            { 3, new Book { Id = 3, Title = "The Catcher in the Rye", Author = new Author { Id = 3 } } }
        };

        public class Query
        {
            [UsePaging]
            public Book[] GetBooks()
                => s_books.Values.ToArray();

            [Lookup]
            public Book? GetBook(int id)
                => s_books.TryGetValue(id, out var book) ? book : null;
        }

        public class Book
        {
            public int Id { get; set; }

            public required string Title { get; set; }

            public required Author Author { get; set; }
        }

        public class Author
        {
            public int Id { get; set; }
        }
    }

    public static class BookInventory
    {
        private static readonly Dictionary<int, Book> s_books = new()
        {
            { 1, new Book { Id = 1, Dimension = new BookDimension { Width = 100, Height = 200 } } },
            { 2, new Book { Id = 2, Dimension = new BookDimension { Width = 150, Height = 300 } } },
            { 3, new Book { Id = 3, Dimension = new BookDimension { Width = 200, Height = 400 } } }
        };

        public class Query
        {
            [Lookup]
            public Book? GetBook(int id)
                => s_books.TryGetValue(id, out var book) ? book : null;
        }

        public class Book
        {
            public int Id { get; set; }

            public required BookDimension Dimension { get; set; }
        }

        public class BookDimension
        {
            public int Width { get; set; }

            public int Height { get; set; }
        }
    }

    public static class BookShipping
    {
        public class Query
        {
            [Lookup]
            public Book? GetBook(int id)
                => new() { Id = id };
        }

        public class Book
        {
            public required int Id { get; set; }

            public int EstimatedDelivery(
                [Require(
                    """
                    {
                      title,
                      width: dimension.width,
                      height: dimension.height
                    }
                    """)]
                 BookDimensionInput dimension)
            {
                return dimension.Width + dimension.Height;
            }
        }
    }

    public class BookDimensionInput
    {
        public required string Title { get; set; }

        public required int Width { get; set; }

        public required int Height { get; set; }
    }
}
