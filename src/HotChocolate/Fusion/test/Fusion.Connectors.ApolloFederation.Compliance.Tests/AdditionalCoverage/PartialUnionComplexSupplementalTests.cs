using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Suites.PartialUnionComplex.A;
using HotChocolate.Fusion.Suites.PartialUnionComplex.B;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class PartialUnionComplexSupplementalTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            new ApolloFederationCompatibilityOptions
            {
                ShareableFieldRuntimeTypeRouting =
                    ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes
            },
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    [Trait("Category", "Supplemental")]
    public Task SharedActions_Should_UseCommonRuntimeTypes_When_RootHasMultipleProviders()
        => RunAsync(
            query: """
                query {
                  sharedActions {
                    __typename
                    ... on Common {
                      label
                    }
                    ... on OnlyA {
                      a
                    }
                    ... on OnlyB {
                      b
                    }
                  }
                }
                """,
            expectedData: """
                {
                  "sharedActions": [
                    {
                      "__typename": "Common",
                      "label": "common label"
                    },
                    {
                      "__typename": "OnlyA",
                      "a": null
                    }
                  ]
                }
                """);
}
