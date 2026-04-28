using HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;
using HotChocolate.Fusion.Suites.UnionInterfaceDistributed.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>union-interface-distributed</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two subgraphs (<c>a</c>, <c>b</c>)
/// verify that unions and interfaces distributed across subgraphs compose
/// correctly. Subgraph <c>a</c> defines <c>Oven</c> without interfaces, while
/// subgraph <c>b</c> extends <c>Oven</c> to implement <c>Node</c> and
/// <c>WithWarranty</c>.
/// </summary>
public sealed class UnionInterfaceDistributedTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
            (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync));

    [Fact(Skip = "Gateway does not resolve cross-subgraph interface implementations on union members.")]
    public Task Products_As_Node_Id() => RunAsync(
        query: """
            {
              products {
                ... on Node {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "oven1" },
                { "id": "oven2" },
                { "id": "toaster1" },
                { "id": "toaster2" }
              ]
            }
            """);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Nodes_Toaster_Warranty_And_Oven_Id() => RunAsync(
        query: """
            {
              nodes {
                ... on Toaster {
                  warranty
                }
                ... on Oven {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "nodes": [
                { "warranty": 3 },
                { "warranty": 4 }
              ]
            }
            """);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Nodes_Id() => RunAsync(
        query: """
            {
              nodes {
                id
              }
            }
            """,
        expectedData: """
            {
              "nodes": [
                { "id": "toaster1" },
                { "id": "toaster2" }
              ]
            }
            """);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Nodes_As_Node_Id() => RunAsync(
        query: """
            {
              nodes {
                ... on Node {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "nodes": [
                { "id": "toaster1" },
                { "id": "toaster2" }
              ]
            }
            """);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Nodes_As_Node_WithWarranty() => RunAsync(
        query: """
            {
              nodes {
                ... on Node {
                  ... on WithWarranty {
                    warranty
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "nodes": [
                { "warranty": 3 },
                { "warranty": 4 }
              ]
            }
            """);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Nodes_Nested_Node_WithWarranty() => RunAsync(
        query: """
            {
              nodes {
                ... on Node {
                  id
                  ... on Node {
                    id
                    ... on WithWarranty {
                      warranty
                    }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "nodes": [
                { "id": "toaster1", "warranty": 3 },
                { "id": "toaster2", "warranty": 4 }
              ]
            }
            """);

    [Fact]
    public Task Toasters_Fragment_Warranty() => RunAsync(
        query: """
            {
              toasters {
                ... ToasterFields
              }
            }

            fragment ToasterFields on Toaster {
              warranty
            }
            """,
        expectedData: """
            {
              "toasters": [
                { "warranty": 3 },
                { "warranty": 4 }
              ]
            }
            """);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Node_Oven_Warranty() => RunAsync(
        query: """
            {
              node(id: "oven1") {
                ... on Oven {
                  warranty
                }
              }
            }
            """,
        expectedData: """
            {
              "node": null
            }
            """,
        expectsErrors: true);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Node_Oven_As_Toaster_Warranty() => RunAsync(
        query: """
            {
              node(id: "oven1") {
                ... on Toaster {
                  warranty
                }
              }
            }
            """,
        expectedData: """
            {
              "node": null
            }
            """,
        expectsErrors: true);

    [Fact(Skip = "Gateway composition drops query fields returning interface types.")]
    public Task Node_Toaster_Warranty() => RunAsync(
        query: """
            {
              node(id: "toaster1") {
                ... on Toaster {
                  warranty
                }
              }
            }
            """,
        expectedData: """
            {
              "node": {
                "warranty": 3
              }
            }
            """);
}
