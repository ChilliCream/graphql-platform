using HotChocolate.Fusion.Suites.Node.Node;
using HotChocolate.Fusion.Suites.Node.NodeTwo;
using HotChocolate.Fusion.Suites.Node.Types;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>node</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three Apollo Federation
/// subgraphs (<c>node</c>, <c>node-two</c>, <c>types</c>) project the same
/// federated <c>Node</c> interface implemented by <c>Product</c> and
/// <c>Category</c>. The <c>node</c> subgraph owns root fields returning
/// the interface; the <c>types</c> subgraph contributes scalar fields on
/// the concrete implementers.
/// </summary>
public sealed class NodeTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (NodeSubgraph.Name, NodeSubgraph.BuildAsync),
            (NodeTwoSubgraph.Name, NodeTwoSubgraph.BuildAsync),
            (TypesSubgraph.Name, TypesSubgraph.BuildAsync));

    /// <summary>
    /// <c>productNode</c> in the <c>node</c> subgraph returns the federated
    /// interface; the planner enriches the concrete <c>Product</c> with
    /// <c>name</c> and <c>price</c> from the <c>types</c> subgraph.
    /// </summary>
    [Fact]
    public Task ProductNode_Resolves_Interface_Through_Concrete_Type() => RunAsync(
        query: """
            {
              productNode {
                ... on Product {
                  id
                  name
                  __typename
                  price
                }
              }
            }
            """,
        expectedData: """
            {
              "productNode": {
                "id": "p-1",
                "name": "Product 1",
                "__typename": "Product",
                "price": 10
              }
            }
            """);
}
