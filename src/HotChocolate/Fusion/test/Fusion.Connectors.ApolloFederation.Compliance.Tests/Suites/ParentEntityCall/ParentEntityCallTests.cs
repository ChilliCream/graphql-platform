using HotChocolate.Fusion.Suites.ParentEntityCall.A;
using HotChocolate.Fusion.Suites.ParentEntityCall.B;
using HotChocolate.Fusion.Suites.ParentEntityCall.C;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>parent-entity-call</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three Apollo Federation
/// subgraphs cooperate on a single <c>products</c> request: subgraph
/// <c>a</c> owns the root <c>products: [Product!]!</c> field and emits the
/// shareable <c>Product.category</c> together with <c>Category.id</c>;
/// subgraph <c>b</c> mirrors that contribution via the compound
/// <c>Product @key("id pid")</c>; subgraph <c>c</c> contributes the
/// <c>Category.details</c> field exclusively, reachable only through the
/// parent <c>Product.category</c> entity call. The gateway must merge the
/// inline category contributions across <c>a</c> / <c>b</c> with the
/// parent-entity-call routing into <c>c</c>.
/// </summary>
public sealed class ParentEntityCallTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    /// <summary>
    /// The single audit case. Walks every product, requests the
    /// <c>Category.id</c> field that <c>a</c> emits inline together with
    /// the <c>Category.details.products</c> field that lives in <c>c</c>
    /// and is only reachable via the parent <c>Product.category</c>
    /// entity call.
    /// </summary>
    [Fact(Skip = "Composer satisfiability validator cycles on Category.id between subgraphs that both declare Category @key(\"id\") with no non-lookup path. See framework gap parent-entity-call in /workspaces/repo/.work/implement/framework-gaps.md (Fusion.Composition/SatisfiabilityValidator.cs:116).")]
    public Task Products_Resolves_Category_Details_From_Parent_Entity_Call() => RunAsync(
        query: """
            query {
              products {
                id
                category {
                  id
                  details {
                    products
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                {
                  "id": "p1",
                  "category": {
                    "id": "c1",
                    "details": { "products": 2 }
                  }
                },
                {
                  "id": "p2",
                  "category": {
                    "id": "c2",
                    "details": { "products": 1 }
                  }
                },
                {
                  "id": "p3",
                  "category": {
                    "id": "c1",
                    "details": { "products": 2 }
                  }
                }
              ]
            }
            """);
}
