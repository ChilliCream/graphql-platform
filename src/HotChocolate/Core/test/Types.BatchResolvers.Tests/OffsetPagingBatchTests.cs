using HotChocolate.Execution;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the native offset paging batch middleware (hc-0-bpl.4 runtime paging helper, offset
/// paging batch twin): a batch-resolved field carries the generated <c>skip</c>/<c>take</c>
/// arguments, slices each parent's already-loaded products in memory, and isolates aliases that
/// pass different <c>skip</c> values into separate batch dispatches.
/// </summary>
[Collection(PostgresCollectionFixture.DefinitionName)]
public sealed partial class OffsetPagingBatchTests(PostgreSqlResource resource) : BatchScenarioTests
{
    /// <summary>
    /// Used only by coverage discovery (<c>MatrixCoverageTests</c>), which never configures an
    /// executor and so never needs a live Postgres resource.
    /// </summary>
    private OffsetPagingBatchTests() : this(null!) { }

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

    // The offset paging batch twin: two brands under one query still slice correctly per parent
    // once the batch partition key reads "take" instead of unconditionally assuming "first"
    // (PagingHelper.cs, hc-0-bpl.4).
    [Theory]
    [BatchMatrix]
    public async Task UseOffsetPaging_Should_Slice_PerParent_When_FieldIsBatchResolved(DeclarationStyle style)
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
                    products(take: 1) { items { name } }
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
                    "products": {
                      "items": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "products": {
                      "items": [
                        {
                          "name": "Brand 2 P1"
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
    public async Task UseOffsetPaging_Should_Partition_PerAlias_When_SkipDiffers(DeclarationStyle style)
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
                    first: products(skip: 0, take: 1) { items { name } }
                    second: products(skip: 1, take: 1) { items { name } }
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
                    "first": {
                      "items": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ]
                    },
                    "second": {
                      "items": [
                        {
                          "name": "Brand 1 P2"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "first": {
                      "items": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ]
                    },
                    "second": {
                      "items": [
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

    // hc-0-6cq.13 regression: two aliases requesting a different page size normalize to
    // different partition keys, so they dispatch separately even though they share a parent.
    [Theory]
    [BatchMatrix]
    public async Task UseOffsetPaging_Should_Partition_When_TakeDiffers(DeclarationStyle style)
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
                    a: products(take: 1) { items { name } }
                    b: products(take: 2) { items { name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Probe.Invocations.Count);
        Assert.Empty(result.ExpectOperationResult().Errors);
    }

    // hc-0-6cq.13 regression: the same selection occurrence with an identical explicit `take`
    // across two variable sets shares one partition.
    [Theory]
    [BatchMatrix]
    public async Task UseOffsetPaging_Should_Coalesce_When_TakeIsIdenticalAcrossVariableSets(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object?>[] sets =
        [
            new Dictionary<string, object?> { ["take"] = 1 },
            new Dictionary<string, object?> { ["take"] = 1 }
        ];

        // act
        await using var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument("query($take:Int){ brands { name products(take:$take){ items { name } } } }")
                .SetVariableValues(sets)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Empty(batch.Results[0].ExpectOperationResult().Errors);
        Assert.Empty(batch.Results[1].ExpectOperationResult().Errors);
    }

    // hc-0-6cq.13 regression: the same selection occurrence normalizes an omitted `take` to the
    // effective default (min(DefaultPageSize, MaxPageSize) = min(10, 50) = 10 with the paging
    // defaults, since this family sets no schema-level override), coalescing with `take: 10`.
    [Theory]
    [BatchMatrix]
    public async Task UseOffsetPaging_Should_Coalesce_When_OmittedMatchesExplicitTake(DeclarationStyle style)
    {
        // arrange
        await SeedAsync(TestContext.Current.CancellationToken);
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object?>[] sets =
        [
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["take"] = 10 }
        ];

        // act
        await using var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument("query($take:Int){ brands { name products(take:$take){ items { name } } } }")
                .SetVariableValues(sets)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Empty(batch.Results[0].ExpectOperationResult().Errors);
        Assert.Empty(batch.Results[1].ExpectOperationResult().Errors);
    }

    // hc-0-6cq.13 regression: a `take` beyond the max page size (50 by default) errors; the error
    // must stay isolated to its own alias/partition and never poison the valid alias sharing the
    // same parents.
    [Theory]
    [BatchMatrix]
    public async Task UseOffsetPaging_Should_Error_Alone_When_TakeExceedsMaxPageSize(DeclarationStyle style)
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
                    valid: products(take: 2) { items { name } }
                    invalid: products(take: 51) { items { name } }
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
}
