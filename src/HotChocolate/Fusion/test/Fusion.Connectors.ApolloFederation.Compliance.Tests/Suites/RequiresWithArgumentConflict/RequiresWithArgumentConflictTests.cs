using HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.A;
using HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.B;

namespace HotChocolate.Fusion.Suites;

public sealed class RequiresWithArgumentConflictTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    public Task Products_ShippingEstimate_EUR_And_IsExpensiveCategory() => RunAsync(
        query: """
            query {
              products {
                upc
                name
                shippingEstimate
                shippingEstimateEUR
                isExpensiveCategory
              }
            }
            """,
        expectedData: """
            {
              "products": [
                {
                  "upc": "p1",
                  "name": "p-name-1",
                  "shippingEstimate": 110,
                  "shippingEstimateEUR": 220,
                  "isExpensiveCategory": false
                },
                {
                  "upc": "p2",
                  "name": "p-name-2",
                  "shippingEstimate": 440,
                  "shippingEstimateEUR": 880,
                  "isExpensiveCategory": true
                }
              ]
            }
            """);
}
