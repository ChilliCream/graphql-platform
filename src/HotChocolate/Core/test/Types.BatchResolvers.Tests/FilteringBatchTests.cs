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
    /// <summary>
    /// Used only by coverage discovery (<c>MatrixCoverageTests</c>), which never configures an
    /// executor and so never needs a live Postgres resource.
    /// </summary>
    private FilteringBatchTests() : this(null!) { }

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
        // ObjectFieldDescriptor.GetTypeRef(elementType) (hc-0-bpl.8, 8011a504af) infers member
        // nullability for a reflection-declared batch list field, so the Attribute cell prints
        // the same non-null shape as SourceGenerated and Fluent.
        printed.MatchInlineSnapshot("products(where: FilteringProductFilterInput): [FilteringProduct!]!");
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
        var dispatches = Probe.Invocations.Count(i => i.MemberName != "Predicate");
        Assert.Equal(2, dispatches);
        var predicates = Probe.Invocations
            .Where(i => i.MemberName == "Predicate")
            .Select(i => i.Keys[0]?.ToString())
            .ToArray();
        Assert.Equal(2, predicates.Length);
        Assert.All(predicates, Assert.NotNull);
        Assert.NotEqual(predicates[0], predicates[1]);
        string.Join("\n", predicates).MatchInlineSnapshot(
            """
            _s0 => (_s0.Name == ExpressionParameter { p = P2 }.p)
            _s0 => (_s0.Name == ExpressionParameter { p = P1 }.p)
            """);
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
