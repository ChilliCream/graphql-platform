using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AliasBatchingTests : FusionTestBase
{
    [Fact]
    public async Task BatchedLookup_Should_SendOneAliasedRequest_When_AliasBatchingEnabled()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            aliasBatching: true);

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
        // The snapshot captures the single alias-batched outbound request to subgraph b.
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchedLookup_Should_ProduceIdenticalResult_When_ComparedToVariableBatching()
    {
        // arrange
        const string query =
            """
            {
              books {
                rating
              }
            }
            """;

        // act
        var variableBatched = await ExecuteAsync(query, aliasBatching: false);
        var aliasBatched = await ExecuteAsync(query, aliasBatching: true);

        // assert
        // The result tree must be byte-identical regardless of the batching transport.
        Assert.Equal(variableBatched, aliasBatched);
    }

    [Fact]
    public async Task OperationBatchedLookup_Should_ProduceIdenticalResult_When_ComparedToVariableBatching()
    {
        // arrange
        // Two sibling lookups to b are merged into one operation batch, exercising the
        // multi-operation alias merge end to end.
        const string query =
            """
            {
              books {
                a: author {
                  a: name
                }
                b: author {
                  b: name(postFix: "2")
                }
              }
            }
            """;

        // act
        var variableBatched = await ExecuteAsync(query, aliasBatching: false);
        var aliasBatched = await ExecuteAsync(query, aliasBatching: true);

        // assert
        Assert.Equal(variableBatched, aliasBatched);
    }

    private async Task<string> ExecuteAsync(string query, bool aliasBatching)
    {
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            aliasBatching: aliasBatching);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(query);

        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        var body = await response.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        // Reformat so incidental whitespace differences do not mask a true tree comparison.
        using var document = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(
            document,
            new JsonSerializerOptions { WriteIndented = true });
    }

    public static class SourceSchema1
    {
        public class Query
        {
            private readonly OrderedDictionary<int, Book> _books =
                new() { [1] = new Book(1, "C# in Depth"), [2] = new Book(2, "The Lord of the Rings") };

            public IEnumerable<Book> GetBooks()
                => _books.Values;

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
        }

        public record Author([property: ID] int Id)
        {
            public string GetName(string? postFix = null)
            {
                var name = "Author " + Id;

                if (string.IsNullOrEmpty(postFix))
                {
                    return name;
                }

                return name + " - " + postFix;
            }
        }
    }
}
