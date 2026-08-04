using HotChocolate.Fusion.Suites.ComplexEntityCall.Link;
using HotChocolate.Fusion.Suites.ComplexEntityCall.List;
using HotChocolate.Fusion.Suites.ComplexEntityCall.Price;
using HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class ComplexEntityCallSupplementalTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (LinkSubgraph.Name, LinkSubgraph.BuildAsync),
            (ListSubgraph.Name, ListSubgraph.BuildAsync),
            (PriceSubgraph.Name, PriceSubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync));

    [Fact]
    [Trait("Category", "Supplemental")]
    public Task TopProducts_Should_ResolveNestedCategoryKey_When_Queried() => RunAsync(
        query: """
            query {
              topProducts {
                products {
                  id
                  category {
                    mainProduct { id }
                    id
                    tag
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "topProducts": {
                "products": [
                  {
                    "id": "1",
                    "category": {
                      "mainProduct": { "id": "1" },
                      "id": "c1",
                      "tag": "t1"
                    }
                  },
                  {
                    "id": "2",
                    "category": {
                      "mainProduct": { "id": "2" },
                      "id": "c2",
                      "tag": "t2"
                    }
                  }
                ]
              }
            }
            """);
}
