using HotChocolate.Execution;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the native cursor paging batch middleware (hc-0-bpl.4 runtime paging helper,
/// hc-0-jyk.2 generator default/selection binding) for the classic <c>UsePaging</c> connection
/// model: a batch-resolved field carries the generated cursor arguments, partitions aliases that
/// pass different effective paging arguments into separate batch dispatches, coalesces the same
/// selection across variable sets that normalize to the same effective arguments, and maps
/// per-selection paging arguments when the resolver takes a <c>PagingArguments</c> parameter.
/// </summary>
public sealed partial class CursorPagingBatchTests : BatchScenarioTests
{
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
    }

    [Theory]
    [BatchMatrix]
    public async Task UsePaging_Should_Coalesce_When_PagingArgumentsAreIdentical(DeclarationStyle style)
    {
        // arrange
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
}
