using HotChocolate.Execution.Configuration;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Runs the worked examples from the How Cost Is Calculated section of the Fusion
/// cost-analysis docs page in report mode and snapshots the reported field cost and
/// type cost.
/// </summary>
public sealed class DocsExamplesTests : FusionTestBase
{
    private const string Schema =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true, slicingArgumentDefaultValue: Int) on FIELD_DEFINITION

        type Query {
          book: Book @cost(weight: "10")
          books(first: Int, last: Int): BooksConnection
            @cost(weight: "10")
            @listSize(
              assumedSize: 50
              slicingArguments: ["first", "last"]
              slicingArgumentDefaultValue: 10
              sizedFields: ["edges", "nodes"]
            )
        }

        type Book {
          title: String
          author: Author
        }

        type Author {
          name: String
        }

        type BooksConnection {
          edges: [BooksEdge]
          nodes: [Book]
        }

        type BooksEdge {
          node: Book
        }
        """;

    private static readonly Uri s_endpoint = new("http://localhost:5000/graphql");

    [Fact]
    public async Task HowCostIsCalculatedDocs_FieldCostExample_Should_ReportDocumentedCost()
    {
        // arrange
        using var server = CreateSourceSchema("A", ConfigureSchema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)], includeOperationPlan: false);
        var request = new OperationRequest(
            """
            {
              book {
                title
                author {
                  name
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request),
            TestContext.Current.CancellationToken);

        // assert
        var result = await response.ReadAsResultAsync(TestContext.Current.CancellationToken);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "book": {
                  "title": "C# in Depth",
                  "author": {
                    "name": "Jon Skeet"
                  }
                }
              },
              "extensions": {
                "operationCost": {
                  "fieldCost": 11,
                  "typeCost": 3
                }
              }
            }
            """);
    }

    [Fact]
    public async Task HowCostIsCalculatedDocs_PaginatedExample_Should_ReportDocumentedCost()
    {
        // arrange
        using var server = CreateSourceSchema("A", ConfigureSchema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)], includeOperationPlan: false);
        var request = new OperationRequest(
            """
            {
              books(first: 50) {
                edges {
                  node {
                    title
                    author {
                      name
                    }
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request),
            TestContext.Current.CancellationToken);

        // assert
        var result = await response.ReadAsResultAsync(TestContext.Current.CancellationToken);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": {
                  "edges": []
                }
              },
              "extensions": {
                "operationCost": {
                  "fieldCost": 111,
                  "typeCost": 152
                }
              }
            }
            """);
    }

    private static void ConfigureSchema(IRequestExecutorBuilder builder)
        => builder
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "book", _ => new Book("C# in Depth", new Author("Jon Skeet")))
            .AddResolver("Query", "books", _ => new BooksConnection([]))
            .AddResolver("Book", "title", ctx => ctx.Parent<Book>().Title)
            .AddResolver("Book", "author", ctx => ctx.Parent<Book>().Author)
            .AddResolver("Author", "name", ctx => ctx.Parent<Author>().Name)
            .AddResolver("BooksConnection", "edges", ctx => ctx.Parent<BooksConnection>().Edges)
            .AddResolver("BooksConnection", "nodes", _ => Array.Empty<Book>())
            .AddResolver("BooksEdge", "node", _ => (Book?)null);

    private static GraphQLHttpRequest WithCostHeader(OperationRequest request)
        => new(request, s_endpoint)
        {
            OnMessageCreated = (_, message, _) => message.Headers.Add("GraphQL-Cost", "report")
        };

    public sealed record Book(string Title, Author Author);

    public sealed record Author(string Name);

    public sealed record BooksEdge(Book? Node);

    public sealed record BooksConnection(IReadOnlyList<BooksEdge> Edges);
}
