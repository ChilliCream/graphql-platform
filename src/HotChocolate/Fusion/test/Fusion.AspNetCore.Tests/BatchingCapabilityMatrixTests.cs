using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Pins the gateway to source schema wire format that every combination of the batching
/// capabilities produces for one and the same plan. The query fetches two distinct lookup
/// operations from the lookup source schema, each of which carries two variable sets, so
/// every combination is exercised on both batching dimensions: the variable sets of one
/// operation and the distinct operations of one batch.
/// </summary>
public class BatchingCapabilityMatrixTests : FusionTestBase
{
    private const string Query =
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

    [Theory]
    [InlineData(SourceSchemaClientCapabilities.None)]
    [InlineData(SourceSchemaClientCapabilities.VariableBatching)]
    [InlineData(SourceSchemaClientCapabilities.RequestBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.VariableBatching
        | SourceSchemaClientCapabilities.RequestBatching)]
    [InlineData(SourceSchemaClientCapabilities.AliasBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching
        | SourceSchemaClientCapabilities.VariableBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching
        | SourceSchemaClientCapabilities.RequestBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching
        | SourceSchemaClientCapabilities.VariableBatching
        | SourceSchemaClientCapabilities.RequestBatching)]
    // The None row pins a known pre-existing defect: the client flattens a multi-set operation into
    // an OperationBatchRequest JSON array even though the source schema declares no request batching
    // capability (HttpSourceSchemaClient.CreateHttpRequest, no-variable-batching branch). The row
    // records the actual behavior, not the intended contract; the defect is tracked separately.
    public async Task BatchedLookups_Should_UseTheProtocolTheCapabilitiesSelect(
        SourceSchemaClientCapabilities capabilities)
    {
        // arrange
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

        var request = new OperationRequest(Query);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // every combination has to resolve the same data, so all rows share one inline snapshot of
        // the response data; only the wire format below is pinned per combination.
        var responseBody = await result.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        using var responseDocument = JsonDocument.Parse(responseBody);
        responseDocument.RootElement.GetProperty("data").MatchInlineSnapshot(
            """
            {
              "books": [
                {
                  "a": {
                    "a": "Author 1"
                  },
                  "b": {
                    "b": "Author 1 - 2"
                  }
                },
                {
                  "a": {
                    "a": "Author 2"
                  },
                  "b": {
                    "b": "Author 2 - 2"
                  }
                }
              ]
            }
            """);

        await MatchSnapshotAsync(gateway, request, result, postFix: CreateLabel(capabilities));
    }

    /// <summary>
    /// Composes the filename-safe label of a capability combination.
    /// </summary>
    private static string CreateLabel(SourceSchemaClientCapabilities capabilities)
    {
        if (capabilities is SourceSchemaClientCapabilities.None)
        {
            return "None";
        }

        var label = string.Empty;

        if (capabilities.HasFlag(SourceSchemaClientCapabilities.AliasBatching))
        {
            label += "A";
        }

        if (capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching))
        {
            label += "V";
        }

        if (capabilities.HasFlag(SourceSchemaClientCapabilities.RequestBatching))
        {
            label += "R";
        }

        return label;
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
            public Author? GetAuthorById([ID] int id) => new(id);
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
