using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the native cursor paging batch middleware (hc-0-bpl.4 runtime paging helper,
/// hc-0-jyk.2 generator default/selection binding) for the classic <c>UsePaging</c> connection
/// model: a batch-resolved field carries the generated cursor arguments, partitions aliases that
/// pass different effective paging arguments into separate batch dispatches, coalesces the same
/// selection across variable sets that normalize to the same effective arguments, and maps
/// per-selection paging arguments when the resolver takes a <c>PagingArguments</c> parameter.
/// </summary>
[Collection(PostgresCollectionFixture.DefinitionName)]
public sealed partial class CursorPagingBatchTests(PostgreSqlResource resource) : BatchScenarioTests
{
    /// <summary>
    /// Used only by coverage discovery (<c>MatrixCoverageTests</c>), which never configures an
    /// executor and so never needs a live Postgres resource.
    /// </summary>
    private CursorPagingBatchTests() : this(null!) { }

    private readonly PostgreSqlResource _resource = resource;
    private readonly List<string> _capturedSql = [];
    private string _connectionString = null!;

    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    // PROVES-WORKS: PagingHelper.UsePaging already registers a BatchFieldMiddleware and a
    // partition key, so a plain [UsePaging][BatchResolver] field dispatches once per distinct
    // effective `first`/`after` and every alias keeps its own page.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Partition_PerAlias_When_FirstDiffers(DeclarationStyle style)
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
                          "name": "Brand 1 P1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        },
                        {
                          "name": "Brand 1 P2"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        },
                        {
                          "name": "Brand 2 P2"
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

    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Coalesce_When_PagingArgumentsAreIdentical(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object?>[] sets =
        [
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["first"] = 2 }
        ];

        // act
        await using var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument("query($first:Int){ brands { name products(first:$first){ nodes { name } } } }")
                .SetVariableValues(sets)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Empty(batch.Results[0].ExpectOperationResult().Errors);
        Assert.Empty(batch.Results[1].ExpectOperationResult().Errors);
        new Snapshot()
            .Add(batch.Results[0], "Omitted first")
            .Add(batch.Results[1], "Explicit effective default")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Map_PagingArguments_PerSelection_When_ParameterIsPagingArguments(
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
                    small: pagedProducts(first: 1) { nodes { name } }
                    large: pagedProducts(first: 2) { nodes { name } }
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
                          "name": "Brand 1 P1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        },
                        {
                          "name": "Brand 1 P2"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        },
                        {
                          "name": "Brand 2 P2"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    // Execution proof for the union ConnectionFlags/PagingArguments wiring deferred from
    // hc-0-jyk.2 (comment 16): one paged batch selection occurrence, evaluated over two variable
    // sets whose include conditions differ (one selects totalCount, the other does not). This
    // records the observed batch dispatch count rather than presupposing it, per adam-1's
    // 2026-09-14 planner note.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Dispatch_PerVariableSet_When_IncludeConditionsDiffer(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object?>[] sets =
        [
            new Dictionary<string, object?> { ["includeTotal"] = false },
            new Dictionary<string, object?> { ["includeTotal"] = true }
        ];

        // act
        await using var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($includeTotal:Boolean!) {
                        brands {
                            name
                            pagedProducts(first: 2) {
                                nodes { name }
                                totalCount @include(if: $includeTotal)
                            }
                        }
                    }
                    """)
                .SetVariableValues(sets)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Empty(batch.Results[0].ExpectOperationResult().Errors);
        Assert.Empty(batch.Results[1].ExpectOperationResult().Errors);
        new Snapshot()
            .Add(batch.Results[0], "Without totalCount")
            .Add(batch.Results[1], "With totalCount")
            .Add(Probe.Invocations.Count, "Observed batch dispatch count")
            .MatchMarkdownSnapshot();
    }

    // hc-0-6cq.13 regression: the same selection occurrence normalizes an omitted `first` to the
    // schema's effective default (min(DefaultPageSize, MaxPageSize) = min(10, 10) = 10 here), so
    // it coalesces with an explicit `first: 10` into a single batch dispatch.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Coalesce_When_OmittedMatchesExplicitFirst(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(
            style,
            builder => builder.ModifyPagingOptions(o => o.DefaultPageSize = 10),
            TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object?>[] sets =
        [
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["first"] = 10 }
        ];

        // act
        await using var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument("query($first:Int){ brands { name products(first:$first){ nodes { name } } } }")
                .SetVariableValues(sets)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Empty(batch.Results[0].ExpectOperationResult().Errors);
        Assert.Empty(batch.Results[1].ExpectOperationResult().Errors);
    }

    // hc-0-6cq.13 regression: with a 100/20 DefaultPageSize/MaxPageSize pairing the effective
    // default clamps to the max (min(100, 20) = 20), which the earlier 2/10 pairing never
    // exercised since 2 < 10 there. An omitted `first` still coalesces with the clamped default.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Coalesce_When_OmittedMatchesClampedDefault(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(
            style,
            builder => builder.ModifyPagingOptions(o =>
            {
                o.DefaultPageSize = 100;
                o.MaxPageSize = 20;
            }),
            TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object?>[] sets =
        [
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["first"] = 20 }
        ];

        // act
        await using var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument("query($first:Int){ brands { name products(first:$first){ nodes { name } } } }")
                .SetVariableValues(sets)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Empty(batch.Results[0].ExpectOperationResult().Errors);
        Assert.Empty(batch.Results[1].ExpectOperationResult().Errors);
    }

    // hc-0-6cq.13 regression: with the 100/20 pairing, a `first` beyond the clamped max page size
    // (20) errors; the error must stay isolated to its own alias/partition and never poison the
    // valid alias sharing the same parents.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Error_Alone_When_FirstExceedsClampedMaxPageSize(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(
            style,
            builder => builder.ModifyPagingOptions(o =>
            {
                o.DefaultPageSize = 100;
                o.MaxPageSize = 20;
            }),
            TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                brands {
                    name
                    valid: products(first: 20) { nodes { name } }
                    invalid: products(first: 21) { nodes { name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.NotEmpty(operationResult.Errors);
        Assert.All(operationResult.Errors, e => Assert.Contains("invalid", e.Path?.ToString()));
        result.MatchSnapshot();
    }

    // hc-0-6cq.13 regression: RequirePagingBoundaries forces every dispatch to specify a
    // boundary; an omitted `first`/`last` errors while an explicit sibling alias still returns.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Error_Alone_When_RequirePagingBoundariesAndFirstOmitted(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(
            style,
            builder => builder.ModifyPagingOptions(o => o.RequirePagingBoundaries = true),
            TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                brands {
                    name
                    explicit: products(first: 2) { nodes { name } }
                    omitted: products { nodes { name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.NotEmpty(operationResult.Errors);
        result.MatchSnapshot();
    }

    // hc-0-6cq.13 regression: an empty-string cursor is hashed raw into its own partition and
    // errors there, leaving a sibling alias without paging arguments untouched.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Error_Alone_When_AfterIsEmptyString(DeclarationStyle style)
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
                    valid: products(first: 2) { nodes { name } }
                    invalid: products(after: "") { nodes { name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.NotEmpty(operationResult.Errors);
        result.MatchSnapshot();
    }

    // hc-0-6cq.13 regression: `last` and `first` normalize to different partition keys even when
    // requesting the same page size, so the two aliases dispatch separately.
    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Partition_When_LastDiffersFromFirst(DeclarationStyle style)
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
                    a: products(last: 2) { nodes { name } }
                    b: products(first: 2) { nodes { name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Probe.Invocations.Count);
        Assert.Empty(result.ExpectOperationResult().Errors);
    }

    // hc-0-6cq.13 "IncludeTotalCount=false never splits" is not expressible here: with the option
    // disabled, ConnectionType/CollectionSegmentType (withTotalCount: false) drop the `totalCount`
    // field from the schema entirely, so selecting it is a document validation error (`The field
    // 'totalCount' does not exist...`) for every variable set alike, not a runtime dispatch
    // decision. See the NEEDS-PLANNER task comment recording this; the "on" side of this
    // requirement is already proven above by
    // UsePaging_Should_Dispatch_PerVariableSet_When_IncludeConditionsDiffer.
}
