using HotChocolate.Fusion.Suites.SharedRoot.Category;
using HotChocolate.Fusion.Suites.SharedRoot.Name;
using HotChocolate.Fusion.Suites.SharedRoot.Price;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>shared-root</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three Apollo Federation
/// subgraphs (<c>category</c>, <c>name</c>, <c>price</c>) all expose the
/// same shareable <c>Query.product</c> and <c>Query.products</c> root fields
/// returning a non-keyed <c>Product</c>. Each subgraph contributes a
/// different field on <c>Product</c>; the planner must split a single query
/// across all three subgraphs.
/// </summary>
public sealed class SharedRootTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (CategorySubgraph.Name, CategorySubgraph.BuildAsync),
            (NameSubgraph.Name, NameSubgraph.BuildAsync),
            (PriceSubgraph.Name, PriceSubgraph.BuildAsync));

    /// <summary>
    /// Single-product query; all three subgraphs contribute fields via the
    /// shared root.
    /// </summary>
    [Fact]
    public Task Product_Composes_Fields_From_Three_Subgraphs() => RunAsync(
        query: """
            query {
              product {
                id
                name { id brand model }
                category { id name }
                price { id amount currency }
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "id": "1",
                "name": { "id": "1", "brand": "Brand 1", "model": "Model 1" },
                "price": { "id": "1", "amount": 1000, "currency": "USD" },
                "category": { "id": "1", "name": "Category 1" }
              }
            }
            """);

    /// <summary>
    /// List form of the same composition; verifies the planner stitches
    /// list elements across subgraphs without an entity lookup.
    /// </summary>
    [Fact(Skip = "Planner does not zip parallel shareable list root queries across subgraphs without an entity lookup. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Products_Composes_Fields_From_Three_Subgraphs() => RunAsync(
        query: """
            query {
              products {
                id
                name { id brand model }
                category { id name }
                price { id amount currency }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                {
                  "id": "1",
                  "name": { "id": "1", "brand": "Brand 1", "model": "Model 1" },
                  "price": { "id": "1", "amount": 1000, "currency": "USD" },
                  "category": { "id": "1", "name": "Category 1" }
                }
              ]
            }
            """);
}
