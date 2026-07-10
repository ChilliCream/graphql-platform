using HotChocolate.Fusion.Suites.MysteriousExternal.Price;
using HotChocolate.Fusion.Suites.MysteriousExternal.Product;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>mysterious-external</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The gateway composes a
/// <c>product</c> subgraph (which owns <c>Product.id</c> and <c>Product.name</c>)
/// and a <c>price</c> subgraph (which extends <c>Product</c> by <c>id @external</c>
/// and owns <c>Product.price</c>). Queries touch both subgraphs via entity calls
/// to merge name and price fields.
/// </summary>
public sealed class MysteriousExternalTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (PriceSubgraph.Name, PriceSubgraph.BuildAsync),
            (ProductSubgraph.Name, ProductSubgraph.BuildAsync));

    [Fact]
    public Task CheapestProduct_Resolves_Name_From_Product_Subgraph() => RunAsync(
        query: """
            {
              cheapestProduct {
                id
                price
                name
              }
            }
            """,
        expectedData: """
            {
              "cheapestProduct": {
                "id": "1",
                "price": 100,
                "name": "name-1"
              }
            }
            """);

    [Fact]
    public Task Products_Resolve_Price_From_Price_Subgraph() => RunAsync(
        query: """
            {
              products {
                name
                price
                id
              }
            }
            """,
        expectedData: """
            {
              "products": [
                {
                  "name": "name-1",
                  "price": 100,
                  "id": "1"
                },
                {
                  "name": "name-2",
                  "price": 200,
                  "id": "2"
                }
              ]
            }
            """);
}
