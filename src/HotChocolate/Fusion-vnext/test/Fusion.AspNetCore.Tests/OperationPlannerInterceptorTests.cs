using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class OperationPlannerInterceptorTests : FusionTestBase
{
    [Fact]
    public async Task Intercept_Plan_With_Single_Interceptor()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>());

        var interceptor1 = new MockInterceptor();

        // act
        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b
                .AddOperationPlannerInterceptor(_ => interceptor1));

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

        // assert
        Assert.True(interceptor1.HasHitOnAfterPlanCompleted);
    }

    [Fact]
    public async Task Intercept_Plan_With_Multiple_Interceptors()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>());

        var interceptor1 = new MockInterceptor();
        var interceptor2 = new MockInterceptor();

        // act
        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b
                .AddOperationPlannerInterceptor(_ => interceptor1)
                .AddOperationPlannerInterceptor(_ => interceptor2));

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

        // assert
        Assert.True(interceptor1.HasHitOnAfterPlanCompleted);
        Assert.True(interceptor2.HasHitOnAfterPlanCompleted);
    }

    private class MockInterceptor : IOperationPlannerInterceptor
    {
        public bool HasHitOnAfterPlanCompleted;

        public void OnAfterPlanCompleted(
            OperationDocumentInfo operationDocumentInfo,
            OperationPlan plan)
        {
            HasHitOnAfterPlanCompleted = true;
        }
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
