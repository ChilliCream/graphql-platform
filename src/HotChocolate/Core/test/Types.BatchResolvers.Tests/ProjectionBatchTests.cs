namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the native projection batch middleware (hc-0-bpl.3) alongside filtering and sorting: a
/// batch-resolved child field still applies its own where/order/select even though its parent is
/// itself projected (the dead regular pipeline's SkipFilteringKey/SkipSortingKey optimizer never
/// sees a batch child, so the batch middleware always applies its own arguments). The IQueryable
/// parent is a shared plain <see cref="ProjectionQuery"/> in every declaration style; it is not
/// itself the thing under test.
/// </summary>
public sealed partial class ProjectionBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Execute_When_ParentUsesProjection_And_FieldUsesFiltering(
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
                    filteredProducts(where: { name: { endsWith: "P1" } }) { name }
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
                    "filteredProducts": [
                      {
                        "name": "Brand 1 P1"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "filteredProducts": [
                      {
                        "name": "Brand 2 P1"
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
    public async Task BatchResolver_Should_Execute_When_ParentUsesProjection_And_FieldUsesSorting(
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
                    sortedProducts(order: [{ name: DESC }]) { name }
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
                    "sortedProducts": [
                      {
                        "name": "Brand 1 P2"
                      },
                      {
                        "name": "Brand 1 P1"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "sortedProducts": [
                      {
                        "name": "Brand 2 P2"
                      },
                      {
                        "name": "Brand 2 P1"
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
    public async Task UseProjection_Should_Project_PerParent_When_FieldIsBatchResolved(DeclarationStyle style)
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
                    projectedProducts { name }
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
                    "projectedProducts": [
                      {
                        "name": "Brand 1 P1"
                      },
                      {
                        "name": "Brand 1 P2"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "projectedProducts": [
                      {
                        "name": "Brand 2 P1"
                      },
                      {
                        "name": "Brand 2 P2"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }
}
