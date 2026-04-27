using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>provides-on-interface</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three subgraphs
/// (<c>a</c>, <c>b</c>, <c>c</c>) verify that <c>@provides</c> works
/// correctly with interface types and inline fragments.
/// </summary>
public sealed class ProvidesOnInterfaceTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
            (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync),
            (SubgraphCSubgraph.Name, SubgraphCSubgraph.BuildAsync));

    /// <summary>
    /// <c>media</c> returns animals with id and name. Subgraph <c>b</c>
    /// provides both fields inline via <c>@provides(fields: "animals { id name }")</c>.
    /// </summary>
    [Fact]
    public Task Media_Animals_With_Id_And_Name() => RunAsync(
        query: """
            query {
              media {
                id
                animals {
                  id
                  name
                }
              }
            }
            """,
        expectedData: """
            {
              "media": {
                "id": "m1",
                "animals": [
                  { "id": "a1", "name": "Fido" },
                  { "id": "a2", "name": "Whiskers" }
                ]
              }
            }
            """);

    /// <summary>
    /// <c>media</c> returns animals with id, name, and Cat-specific age.
    /// The <c>age</c> field requires an entity call to subgraph <c>c</c>.
    /// </summary>
    [Fact]
    public Task Media_Animals_With_Cat_Age() => RunAsync(
        query: """
            query {
              media {
                id
                animals {
                  id
                  name
                  ... on Cat {
                    age
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "media": {
                "id": "m1",
                "animals": [
                  { "id": "a1", "name": "Fido" },
                  { "id": "a2", "name": "Whiskers", "age": 6 }
                ]
              }
            }
            """);
}
