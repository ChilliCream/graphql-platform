using HotChocolate.Fusion.Suites.PartialUnion.A;
using HotChocolate.Fusion.Suites.PartialUnion.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>partial-union</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The <c>a</c> subgraph declares
/// the full <c>Action = Alpha | Beta | Gamma</c> union and owns
/// <c>Query.getResponse</c>, while the <c>b</c> subgraph contributes only a
/// partial union (<c>Action = Alpha</c>) on the shared <c>@shareable Response</c>
/// type. None of the union members declare a <c>@key</c>, so <c>Beta</c> and
/// <c>Gamma</c> can only be resolved from <c>a</c>. A correct gateway keeps every
/// inline fragment and resolves the field from the subgraph that owns the full
/// union.
/// </summary>
public sealed class PartialUnionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// Selects all three union members. <c>Beta</c> and <c>Gamma</c> exist only
    /// in the primary subgraph and have no <c>@key</c>, so they can only be
    /// resolved there. A correct gateway keeps every inline fragment and returns
    /// all three.
    /// </summary>
    [Fact]
    public Task GetResponse_ReturnsAllUnionMembers() => RunAsync(
        query: """
            query {
              getResponse {
                message
                actions {
                  __typename
                  ... on Alpha {
                    id
                    value
                  }
                  ... on Beta {
                    id
                    name
                    details
                  }
                  ... on Gamma {
                    id
                    label
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "getResponse": {
                "message": "Hello, Federation!",
                "actions": [
                  {
                    "__typename": "Alpha",
                    "id": "alpha-1",
                    "value": "alpha value"
                  },
                  {
                    "__typename": "Beta",
                    "id": "beta-1",
                    "name": "beta name",
                    "details": "beta details"
                  },
                  {
                    "__typename": "Gamma",
                    "id": "gamma-1",
                    "label": "gamma label"
                  }
                ]
              }
            }
            """);

    /// <summary>
    /// Selects only the members missing from the secondary subgraph; they must
    /// still resolve from the primary subgraph.
    /// </summary>
    [Fact]
    public Task GetResponse_ReturnsMissingMembers() => RunAsync(
        query: """
            query {
              getResponse {
                actions {
                  __typename
                  ... on Beta {
                    name
                  }
                  ... on Gamma {
                    label
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "getResponse": {
                "actions": [
                  {
                    "__typename": "Alpha"
                  },
                  {
                    "__typename": "Beta",
                    "name": "beta name"
                  },
                  {
                    "__typename": "Gamma",
                    "label": "gamma label"
                  }
                ]
              }
            }
            """);
}
