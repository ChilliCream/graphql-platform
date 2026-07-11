using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Suites.PartialUnionComplex.A;
using HotChocolate.Fusion.Suites.PartialUnionComplex.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>partial-union-complex</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs (<c>a</c>, <c>b</c>) share the <c>Container</c> entity and the
/// <c>Wrapper.actions</c> union. The union <c>Action</c> exposes a member
/// (<c>OnlyA</c>) only in <c>a</c> and a member (<c>OnlyB</c>) only in <c>b</c>,
/// with <c>Common</c> shared. The gateway must keep the union member fragments
/// that are common to every viable provider of a shareable field. Provider scope
/// may expand across the keyed <c>Container</c>, but narrows again below provider-specific fields.
/// </summary>
public sealed class PartialUnionComplexTests : ComplianceTestBase
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
    public Task RootA_Wrapper_Actions_ResolvesCommonAndOnlyA() => RunAsync(
        query: """
            query {
              rootA {
                wrapper {
                  actions {
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
              }
            }
            """,
        expectedData: """
            {
              "rootA": {
                "wrapper": {
                  "actions": [
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
              }
            }
            """);

    [Fact]
    public Task RootB_Wrapper_Actions_ResolvesCommonAndOnlyB() => RunAsync(
        query: """
            query {
              rootB {
                wrapper {
                  actions {
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
              }
            }
            """,
        expectedData: """
            {
              "rootB": {
                "wrapper": {
                  "actions": [
                    {
                      "__typename": "Common",
                      "label": "common label"
                    },
                    {
                      "__typename": "OnlyB",
                      "b": null
                    }
                  ]
                }
              }
            }
            """);

    [Fact]
    public Task RootA_Wrapper_Actions_OnlyBFragment_ReturnsTypenamesOnly() => RunAsync(
        query: """
            query {
              rootA {
                wrapper {
                  actions {
                    __typename
                    ... on OnlyB {
                      b
                    }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "rootA": {
                "wrapper": {
                  "actions": [
                    {
                      "__typename": "Common"
                    },
                    {
                      "__typename": "OnlyA"
                    }
                  ]
                }
              }
            }
            """);

    [Fact]
    public Task Shared_Wrapper_Actions_ResolvesCommonOnly() => RunAsync(
        query: """
            query {
              shared {
                wrapper {
                  actions {
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
              }
            }
            """,
        expectedData: """
            {
              "shared": {
                "wrapper": {
                  "actions": [
                    {
                      "__typename": "Common",
                      "label": "common label"
                    }
                  ]
                }
              }
            }
            """);

    [Fact]
    public Task SharedRoot_Actions_UsesCommonRuntimeTypesAcrossRootProviders() => RunAsync(
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

    [Fact]
    public Task RootA_BWrapper_Actions_EntityHopResolvesCommonAndOnlyB() => RunAsync(
        query: """
            query {
              rootA {
                bWrapper {
                  actions {
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
              }
            }
            """,
        expectedData: """
            {
              "rootA": {
                "bWrapper": {
                  "actions": [
                    {
                      "__typename": "Common",
                      "label": "common label"
                    },
                    {
                      "__typename": "OnlyB",
                      "b": "only b"
                    }
                  ]
                }
              }
            }
            """);
}
