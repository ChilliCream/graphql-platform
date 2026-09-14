using HotChocolate.Language.Utilities;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the native filtering batch middleware (hc-0-bpl.3): a batch-resolved field carries the
/// generated <c>where</c> argument, filters each parent's already-loaded products in memory, and
/// isolates aliases that pass different <c>where</c> values into separate batch dispatches.
/// </summary>
[Collection(PostgresCollectionFixture.DefinitionName)]
public sealed partial class FilteringBatchTests(PostgreSqlResource resource) : BatchScenarioTests
{
    private readonly PostgreSqlResource _resource = resource;
    private readonly List<string> _capturedSql = [];
    private string _connectionString = null!;

    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task UseFiltering_Should_Expose_WhereArgument_When_FieldIsBatchResolved(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        var products = executor.Schema.Types.GetType<ObjectType>("FilteringBrand").Fields["products"];
        var printed = products.ToSyntaxNode().Print(false);

        // assert
        var expected = style switch
        {
            DeclarationStyle.SourceGenerated => "products(where: FilteringProductFilterInput): [FilteringProduct!]!",
            _ => "products(where: FilteringProductFilterInput): [FilteringProduct]"
        };
        printed.MatchInlineSnapshot(expected);
    }

    [Theory]
    [BatchMatrix]
    public async Task UseFiltering_Should_Filter_PerParent_When_FieldIsBatchResolved(DeclarationStyle style)
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
                    products(where: { name: { eq: "P1" } }) {
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
                        "name": "P1"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "products": [
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
    public async Task UseFiltering_Should_Partition_PerAlias_When_WhereDiffers(DeclarationStyle style)
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
                    a: products(where: { name: { eq: "P1" } }) { name }
                    b: products(where: { name: { eq: "P2" } }) { name }
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
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "a": [
                      {
                        "name": "P1"
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task GetFilterContext_Should_Return_PerPartition_Predicate_When_UsedInsideBatchResolver(
        DeclarationStyle style)
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
                    a: predicateProducts(where: { name: { eq: "P1" } }) { name }
                    b: predicateProducts(where: { name: { eq: "P2" } }) { name }
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
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "a": [
                      {
                        "name": "P1"
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }
}
