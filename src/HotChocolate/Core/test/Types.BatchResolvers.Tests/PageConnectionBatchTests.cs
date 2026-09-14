using Squadron;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the <c>PageConnection&lt;T&gt;</c> paging model (hc-0-jyk.2 generated default/selection
/// binding) can carry a batch-resolved <c>PagingArguments</c> parameter, kept in a family of its
/// own: mixing a classic <c>UsePaging</c> connection with <c>PageConnection&lt;T&gt;</c> in the
/// same schema collides on the <c>PageInfo</c> type name (HC0065, adam-1's 2026-09-14 planner
/// note) because the two models resolve to the same GraphQL type name for a structurally
/// different C# type. One paging model per schema, per that ruling.
/// </summary>
[Collection(PostgresCollectionFixture.DefinitionName)]
public sealed partial class PageConnectionBatchTests(PostgreSqlResource resource) : BatchScenarioTests
{
    /// <summary>
    /// Used only by coverage discovery (<c>MatrixCoverageTests</c>), which never configures an
    /// executor and so never needs a live Postgres resource.
    /// </summary>
    private PageConnectionBatchTests() : this(null!) { }

    private readonly PostgreSqlResource _resource = resource;
    private readonly List<string> _capturedSql = [];
    private string _connectionString = null!;

    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = Declaration.NotApplicable(
            "no public fluent equivalent of UseConnectionAttribute exists; its batch paging "
            + "validation middleware and partition key wiring are protected internal to "
            + "Types.CursorPagination")
    };

    [Theory]
    [BatchMatrix]
    public async Task UseConnection_Should_Map_PagingArguments_When_ReturnTypeIsPageConnection(DeclarationStyle style)
    {
        // arrange
        if (GetNotApplicableReason(style) is not null)
        {
            return;
        }

        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                brands {
                    name
                    small: products(first: 1) { nodes { name } }
                    large: products(first: 2) { nodes { name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Probe.Invocations.Count);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        },
                        {
                          "name": "Brand 1 Product 2"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        },
                        {
                          "name": "Brand 2 Product 2"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
        var sql = Assert.Single(_capturedSql);
        new Snapshot().Add(sql, "Captured SQL (root brands query)").MatchMarkdownSnapshot();
    }
}
