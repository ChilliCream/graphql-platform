using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class RequireTests : FusionTestBase
{
    [Fact]
    public async Task Requirement_On_Leaf_Field()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableField: String
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableLeafFieldRequirement.Query>());

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
              productById(id: "1") {
                fieldWithNullableRequirement
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
    public async Task Requirement_On_Nullable_Leaf_Field_Returning_Null()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableField: String @null
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableLeafFieldRequirement.Query>());

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
              productById(id: "1") {
                fieldWithNullableRequirement
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
    public async Task Requirement_On_Property_Within_Nullable_Object()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableObject: Wrapper
            }

            type Wrapper {
              field: String!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableObjectFieldRequirement.Query>());

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
              productById(id: "1") {
                fieldWithNullableRequirement
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
    public async Task Requirement_On_Property_Within_Nullable_Object_Returning_Null()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableObject: Wrapper @null
            }

            type Wrapper {
              field: String!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableObjectFieldRequirement.Query>());

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
              productById(id: "1") {
                fieldWithNullableRequirement
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

        using var server4 = CreateSourceSchema(
            "d",
            b => b.AddQueryType<BookGenre.Query>()
                .AddType<BookGenre.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3),
            ("d", server4)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                books {
                  nodes {
                    title
                    estimatedDelivery
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
    public async Task Require_Enumerable_In_List()
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
        using var server4 = CreateSourceSchema(
            "d",
            b => b.AddQueryType<BookGenre.Query>()
                .AddType<BookGenre.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3),
            ("d", server4)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                books {
                  nodes {
                    title
                    genres {
                     name
                    }
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

    [Fact(Skip = "Not yet supported")]
    public async Task Require_On_MutationPayload()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type User {
                id: ID!
                someField: String!
            }

            type Query {
                userById(id: ID!): User @lookup
            }
            """
        );

        var server2 = CreateSourceSchema(
            "B",
            """
            type User {
                id: ID!
                nestedField(someField: String! @require(field: "someField")): NestedType!
            }

            type NestedType {
                otherField: Int!
            }

            type Mutation {
                createUser: CreateUserPayload
            }

            type CreateUserPayload {
                user: User!
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            mutation {
                createUser {
                    user {
                        nestedField {
                            otherField
                        }
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

    private static class NullableLeafFieldRequirement
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById([ID] int id) => new Product(id);
        }

        public record Product([property: ID] int Id)
        {
            public string GetFieldWithNullableRequirement([Require("nullableField")] string? nullableArgument)
            {
                return nullableArgument is null ? "Required field is null" : "Required field is not null";
            }
        }
    }

    private static class NullableObjectFieldRequirement
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById([ID] int id) => new Product(id);
        }

        public record Product([property: ID] int Id)
        {
            public string GetFieldWithNullableRequirement([Require("nullableObject.field")] string? nullableArgument)
            {
                return nullableArgument is null ? "Required field is null" : "Required field is not null";
            }
        }
    }

    public static class BookCatalog
    {
        private static readonly Dictionary<int, Book> s_books = new()
        {
            {
                1, new Book { Id = 1, Title = "The Great Gatsby", Author = new Author { Id = 1 }, GenreIds = [1, 3] }
            },
            { 2, new Book { Id = 2, Title = "1984", Author = new Author { Id = 2 }, GenreIds = [2, 3] } },
            { 3, new Book { Id = 3, Title = "The Catcher in the Rye", Author = new Author { Id = 3 }, GenreIds = [1] } }
        };

        public class Query
        {
            [UsePaging]
            public Book[] GetBooks()
                => s_books.Values.ToArray();

            [Lookup]
            [Shareable]
            public Book? GetBook(int id)
                => s_books.TryGetValue(id, out var book) ? book : null;
        }

        public class Book
        {
            public int Id { get; set; }

            public required string Title { get; set; }

            public required Author Author { get; set; }

            public required IEnumerable<int> GenreIds { get; set; }
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
            [Shareable]
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

    public static class BookGenre
    {
        private static readonly Dictionary<int, Genre> s_books = new()
        {
            { 1, new Genre { Id = 1, Name = "Fiction" } },
            { 2, new Genre { Id = 2, Name = "Science Fiction" } },
            { 3, new Genre { Id = 3, Name = "Classic" } }
        };

        public class Query
        {
            [Lookup]
            [Shareable]
            public Book? GetBook(int id)
                => new() { Id = id };
        }

        public class Genre
        {
            public required int Id { get; set; }
            public required string Name { get; set; }
        }

        public class Book
        {
            public int Id { get; set; }

            public IEnumerable<Genre> Genres(
                [Require("genreIds")] IEnumerable<int> genreIds)
            {
                return genreIds.Select(id => s_books[id]);
            }
        }
    }

    public static class BookShipping
    {
        public class Query
        {
            [Lookup]
            [Shareable]
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
