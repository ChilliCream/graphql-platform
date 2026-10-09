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
        var result = await gateway.Executor.ExecuteAsync(
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
            """,
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

        // guard: no candidate is a ConfigMatchesResult, so its dedicated asset
        // lookup is skipped and the nested lookups must still reach 'search'
        // guard: no candidate matches the other three sibling fragments, so their
        // dedicated asset lookups are skipped at runtime and the nested MeterType
        // and Category lookups must still reach the 'search' subgraph
        capture.Requests.Select(r => $"{r.SubgraphName}: {r.Body}").MatchInlineSnapshot(
            """
            [
              "search: {\"query\":\"query Op_f75113c5_1 {\\n  search {\\n    __typename\\n    ... on ExistingResult {\\n      asset {\\n        id\\n      }\\n    }\\n    ... on ConfigMatchesResult {\\n      asset {\\n        id\\n      }\\n    }\\n    ... on NotSubmissableResult {\\n      asset {\\n        id\\n      }\\n    }\\n    ... on CustomResult {\\n      asset {\\n        id\\n      }\\n    }\\n  }\\n}\"}",
              "assets: {\"query\":\"query($representations: [_Any!]!) {\\n  _entities(representations: $representations) {\\n    ... on Asset {\\n      meterType {\\n        id\\n      }\\n      category {\\n        id\\n      }\\n    }\\n  }\\n}\",\"variables\":{\"representations\":[{\"__typename\":\"Asset\",\"id\":\"1\"},{\"__typename\":\"Asset\",\"id\":\"2\"},{\"__typename\":\"Asset\",\"id\":\"3\"}]}}",
              "search: {\"query\":\"query Op_f75113c5_Batch_a34ec4a33baf69b0($_0_representations:[_Any!]!,$_1_representations:[_Any!]!){_0__entities:_entities(representations:$_0_representations){...on Category{allowedMeterTypes{type}}} _1__entities:_entities(representations:$_1_representations){...on MeterType{type}}}\",\"variables\":{\"_0_representations\":[{\"__typename\":\"Category\",\"id\":\"c1\"},{\"__typename\":\"Category\",\"id\":\"c2\"},{\"__typename\":\"Category\",\"id\":\"c3\"}],\"_1_representations\":[{\"__typename\":\"MeterType\",\"id\":\"m1\"},{\"__typename\":\"MeterType\",\"id\":\"m2\"},{\"__typename\":\"MeterType\",\"id\":\"m3\"}]}}"
            ]
            """);
    }
}
