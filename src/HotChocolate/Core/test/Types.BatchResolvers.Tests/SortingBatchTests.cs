using HotChocolate.Language.Utilities;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the native sorting batch middleware (hc-0-bpl.3): a batch-resolved field carries the
/// generated <c>order</c> argument, orders each parent's already-loaded products in memory, and
/// isolates aliases that pass different <c>order</c> values into separate batch dispatches.
/// </summary>
[Collection(PostgresCollectionFixture.DefinitionName)]
public sealed partial class SortingBatchTests(PostgreSqlResource resource) : BatchScenarioTests
{
    /// <summary>
    /// Used only by coverage discovery (<c>MatrixCoverageTests</c>), which never configures an
    /// executor and so never needs a live Postgres resource.
    /// </summary>
    private SortingBatchTests() : this(null!) { }

    private readonly PostgreSqlResource _resource = resource;
    private readonly List<string> _capturedSql = [];
    private string _connectionString = null!;

    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    protected override Task DisposeDatabaseAsync()
        => _connectionString is null
            ? Task.CompletedTask
            : _resource.DropDatabaseAsync(_connectionString, TestContext.Current.CancellationToken);

    [Theory]
    [BatchMatrix]
    public async Task UseSorting_Should_Expose_OrderArgument_When_FieldIsBatchResolved(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        var products = executor.Schema.Types.GetType<ObjectType>("SortingBrand").Fields["products"];
        var printed = products.ToSyntaxNode().Print(false);

        // assert
        // ObjectFieldDescriptor.GetTypeRef(elementType) (hc-0-bpl.8, 8011a504af) infers member
        // nullability for a reflection-declared batch list field, so the Attribute cell prints
        // the same non-null shape as SourceGenerated and Fluent.
        printed.MatchInlineSnapshot("products(order: [SortingProductSortInput!]): [SortingProduct!]!");
    }

    [Theory]
    [BatchMatrix]
    public async Task UseSorting_Should_Order_PerParent_When_FieldIsBatchResolved(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                brands {
                    name
                    products(order: [{ name: DESC }]) {
                        name
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "products": [
                      {
                        "name": "P2"
                      },
                      {
                        "name": "P1"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "products": [
                      {
                        "name": "P2"
                      },
                      {
                        "name": "P1"
                      }
                    ]
                  }
                ]
              }
            }
            """);
        var sql = Assert.Single(_capturedSql);
        new Snapshot().Add(sql, "Captured SQL (root brands query)").MatchMarkdownSnapshot();
    }

    [Theory]
    [BatchMatrix]
    public async Task UseSorting_Should_Partition_PerAlias_When_OrderDiffers(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                brands {
                    name
                    a: products(order: [{ name: ASC }]) { name }
                    b: products(order: [{ name: DESC }]) { name }
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
                    "a": [
                      {
                        "name": "P1"
                      },
                      {
                        "name": "P2"
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      },
                      {
                        "name": "P1"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "a": [
                      {
                        "name": "P1"
                      },
                      {
                        "name": "P2"
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      },
                      {
                        "name": "P1"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }
}
