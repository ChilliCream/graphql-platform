using HotChocolate.Execution;
using HotChocolate.Fusion.AdditionalCoverage.UnionSiblingPartialSkip.Assets;
using HotChocolate.Fusion.AdditionalCoverage.UnionSiblingPartialSkip.Docs;
using HotChocolate.Fusion.AdditionalCoverage.UnionSiblingPartialSkip.Search;

namespace HotChocolate.Fusion.AdditionalCoverage;

/// <summary>
/// Pins that a partially skipped Apollo entity lookup batch is not mistaken for a
/// skipped execution node. When one sibling's lookup is skipped for absent data
/// while the batch itself succeeds, a later failure of another optional
/// dependency must not cascade-skip the nested lookups of the surviving siblings.
/// </summary>
public sealed class UnionSiblingPartialSkipSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task ApolloEntityLookups_Should_IssueNestedLookups_When_SiblingBatchPartiallySkippedAndOtherBranchFails()
    {
        // arrange
        // the runtime data contains only ExistingResult and OtherResult items, so
        // the ExistingResult2 sibling's asset lookup is skipped inside the sibling
        // lookup batch while the batch itself succeeds; the doc branch targets the
        // 'docs' subgraph whose _entities endpoint fails slowly, so its failure is
        // processed after the sibling batch already completed
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            (SearchSubgraph.Name, SearchSubgraph.BuildAsync),
            (AssetsSubgraph.Name, AssetsSubgraph.BuildAsync),
            (DocsSubgraph.Name, DocsSubgraph.BuildAsync));

        // act
        // the two asset siblings select differing sub-selections so each gets its
        // own lookup; the nested meterType lookups from all branches must still be
        // issued for the surviving siblings
        var result = await gateway.Executor.ExecuteAsync(
            """
            {
              search {
                ... on ExistingResult {
                  asset {
                    meterType {
                      type
                    }
                  }
                }
                ... on ExistingResult2 {
                  asset {
                    meterType {
                      type
                    }
                    statusMessage
                  }
                }
                ... on OtherResult {
                  doc {
                    meterType {
                      type
                    }
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        // the doc branch failed, so errors are expected, but the surviving asset
        // branch must still resolve its nested meterType
        AuditAssertions.Assert(
            result.ToJson(),
            expectedDataJson:
            """
            {
              "search": [
                {
                  "asset": {
                    "meterType": { "type": "type-m1" }
                  }
                },
                {
                  "asset": {
                    "meterType": { "type": "type-m2" }
                  }
                },
                {
                  "doc": {
                    "meterType": null
                  }
                }
              ]
            }
            """,
            expectsErrors: true);

        Assert.Contains(
            capture.Requests,
            r => r.SubgraphName == SearchSubgraph.Name
                && r.Body.Contains("MeterType", StringComparison.Ordinal));
    }
}
