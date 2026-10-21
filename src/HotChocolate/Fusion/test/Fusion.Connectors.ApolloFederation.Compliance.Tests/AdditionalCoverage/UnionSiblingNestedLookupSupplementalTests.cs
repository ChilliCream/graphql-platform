using HotChocolate.Execution;
using HotChocolate.Fusion.AdditionalCoverage.UnionSiblingNestedLookup.Assets;
using HotChocolate.Fusion.AdditionalCoverage.UnionSiblingNestedLookup.Search;

namespace HotChocolate.Fusion.AdditionalCoverage;

/// <summary>
/// The Apollo Federation twin of the union-sibling repro for issue #10217: sibling
/// inline fragments on a union select the same entity field with differing
/// sub-selections, so the planner forms one <c>_entities</c> lookup per sibling.
/// When union member types are absent from the runtime data, their sibling lookups
/// are legitimately skipped, and the nested <c>MeterType</c> and <c>Category</c>
/// lookups for the present members must still be issued.
/// </summary>
public sealed class UnionSiblingNestedLookupSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task ApolloEntityLookups_Should_IssueNestedLookups_When_UnionSiblingSelectionsDifferAndMembersAreAbsent()
    {
        // arrange
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            (SearchSubgraph.Name, SearchSubgraph.BuildAsync),
            (AssetsSubgraph.Name, AssetsSubgraph.BuildAsync));

        // act
        // four sibling fragments select the same 'asset' field: two identical
        // (ExistingResult, CustomResult), one with the entity fields reordered
        // (ConfigMatchesResult) and one with an extra field (NotSubmissableResult);
        // the runtime data contains only CustomResult items
        const string query =
            """
            {
              search {
                ... on ExistingResult {
                  asset {
                    meterType {
                      type
                    }
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                    meterType {
                      type
                    }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType {
                      type
                    }
                    statusMessage
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType {
                      type
                    }
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
              }
            }
            """;
        var result = await gateway.Executor.ExecuteAsync(
            query,
            TestContext.Current.CancellationToken);

        // assert
        AuditAssertions.Assert(
            result.ToJson(),
            expectedDataJson:
            """
            {
              "search": [
                {
                  "asset": {
                    "meterType": { "type": "type-m1" },
                    "category": { "allowedMeterTypes": [ { "type": "type-ac1" } ] }
                  }
                },
                {
                  "asset": {
                    "meterType": { "type": "type-m2" },
                    "category": { "allowedMeterTypes": [ { "type": "type-ac2" } ] }
                  }
                },
                {
                  "asset": {
                    "meterType": { "type": "type-m3" },
                    "category": { "allowedMeterTypes": [ { "type": "type-ac3" } ] }
                  }
                }
              ]
            }
            """,
            expectsErrors: false);

        await ApolloIntegrationSnapshot.MatchAsync(
            new Snapshot(extension: ".yaml"),
            query,
            capture,
            result,
            includeRequestOrder: true);
    }
}
