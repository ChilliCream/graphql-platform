using HotChocolate.Fusion.Suites.KeysMashup.A;
using HotChocolate.Fusion.Suites.KeysMashup.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>keys-mashup</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs share the <c>A</c> entity through four overlapping keys
/// (only one is resolvable on each side) plus a deeply nested key.
/// Subgraph <c>b</c> exposes <c>nameInB</c> via <c>@requires(name)</c>.
/// </summary>
public sealed class KeysMashupTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// Walks <c>b.a[].name</c> (from subgraph <c>a</c>) and
    /// <c>b.a[].nameInB</c> (from subgraph <c>b</c>'s
    /// <c>@requires(name)</c>).
    /// </summary>
    [Fact(Skip = "Planner does not yet route the @requires(name) field through the entity lookup. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task B_Resolves_A_Name_And_NameInB_Via_Requires() => RunAsync(
        query: """
            query {
              b {
                id
                a {
                  id
                  name
                  nameInB
                }
              }
            }
            """,
        expectedData: """
            {
              "b": {
                "id": "100",
                "a": [
                  {
                    "id": "1",
                    "name": "a.1",
                    "nameInB": "b.a.nameInB a.1"
                  }
                ]
              }
            }
            """);
}
