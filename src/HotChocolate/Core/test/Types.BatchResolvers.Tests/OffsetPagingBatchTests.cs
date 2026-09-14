namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the native offset paging batch middleware (hc-0-bpl.4 runtime paging helper, offset
/// paging batch twin): a batch-resolved field carries the generated <c>skip</c>/<c>take</c>
/// arguments, slices each parent's already-loaded products in memory, and isolates aliases that
/// pass different <c>skip</c> values into separate batch dispatches.
/// </summary>
public sealed partial class OffsetPagingBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    // The offset paging batch twin: two brands under one query still slice correctly per parent
    // once the batch partition key reads "take" instead of unconditionally assuming "first"
    // (PagingHelper.cs, hc-0-bpl.4).
    [Theory]
    [BatchMatrix]
    public async Task UseOffsetPaging_Should_Slice_PerParent_When_FieldIsBatchResolved(DeclarationStyle style)
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
    }

    [Theory]
    [BatchMatrix]
    public async Task UseOffsetPaging_Should_Partition_PerAlias_When_SkipDiffers(DeclarationStyle style)
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
}
