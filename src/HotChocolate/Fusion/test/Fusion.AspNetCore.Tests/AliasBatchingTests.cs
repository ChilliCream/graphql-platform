using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AliasBatchingTests : FusionTestBase
{
    [Fact]
    public async Task BatchedLookup_Should_SendOneAliasedRequest_When_AliasBatchingIsEnabled()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.AliasBatching);

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
                rating
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchedLookup_Should_RouteErrorsToTheirItem_When_OneItemFails()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.AliasBatching);

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
                title
                errorRating
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchedLookup_Should_ShareTheClientVariable_When_ItIsForwarded()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.AliasBatching);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query($postFix: String) {
              books {
                author {
                  name(postFix: $postFix)
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["postFix"] = "x" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchedLookup_Should_ProduceTheSameResult_When_ComparedToVariableBatching()
    {
        // arrange
        const string query =
            """
            {
              books {
                rating
                author {
                  name
                }
              }
            }
            """;

        // act
        var variableBatched = await ExecuteAsync(query, SourceSchemaClientCapabilities.Default);
        var aliasBatched = await ExecuteAsync(query, SourceSchemaClientCapabilities.AliasBatching);

        // assert
        Assert.Equal(variableBatched, aliasBatched);
    }

    private async Task<string> ExecuteAsync(
        string query,
        SourceSchemaClientCapabilities capabilities)
    {
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: capabilities);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var response = await client.PostAsync(
            new OperationRequest(query),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        var body = await response.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        // Reformat so incidental whitespace differences do not mask a true tree comparison.
        using var document = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    public static class SourceSchema1
    {
        public class Query
        {
            private readonly OrderedDictionary<int, Book> _books =
                new() { [1] = new Book(1, "C# in Depth"), [2] = new Book(2, "The Lord of the Rings") };

            public IEnumerable<Book> GetBooks() => _books.Values;

            [Lookup, Internal]
            public Book? GetBookById([ID] int id) => _books.GetValueOrDefault(id);

            [Lookup, Internal]
            public Author? GetAuthorById([ID] int id) => null;
        }

        public record Book([property: ID] int Id, string Title)
        {
            public Author? GetAuthor() => new Author(Id);
        }

        public record Author([property: ID] int Id);
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Lookup, Internal]
            public Book? GetBookById([ID] int id) => new(id);

            [Lookup, Internal]
            public Author? GetAuthorById([ID] int id) => new(id);
        }

        public record Book([property: ID] int Id)
        {
            public string GetRating() => Id.ToString();

            public string? GetErrorRating()
                => Id == 2
                    ? throw new GraphQLException("Rating unavailable.")
                    : Id.ToString();
        }

        public record Author([property: ID] int Id)
        {
            public string GetName(string? postFix = null)
            {
                var name = "Author " + Id;

                return string.IsNullOrEmpty(postFix) ? name : name + " - " + postFix;
            }
        }
    }
}
