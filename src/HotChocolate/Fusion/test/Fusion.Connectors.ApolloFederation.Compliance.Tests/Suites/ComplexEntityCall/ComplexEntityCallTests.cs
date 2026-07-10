using HotChocolate.Fusion.Suites.ComplexEntityCall.Link;
using HotChocolate.Fusion.Suites.ComplexEntityCall.List;
using HotChocolate.Fusion.Suites.ComplexEntityCall.Price;
using HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>complex-entity-call</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The gateway composes four
/// Apollo Federation subgraphs (<c>link</c>, <c>list</c>, <c>price</c>,
/// <c>products</c>) that share the <c>Product</c> / <c>ProductList</c> /
/// <c>Category</c> entities via several nested-path <c>@key</c> directives,
/// including the list-typed <c>products { id pid }</c> and the deeply nested
/// <c>products { id pid category { id tag } } selected { id }</c>.
/// </summary>
public sealed class ComplexEntityCallTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (LinkSubgraph.Name, LinkSubgraph.BuildAsync),
            (ListSubgraph.Name, ListSubgraph.BuildAsync),
            (PriceSubgraph.Name, PriceSubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync));

    /// <summary>
    /// Verifies the four-subgraph composition accepts the nested and nested-list
    /// <c>@key</c> directives from the audit fixtures and that a query
    /// traversing the nested-object <c>Category @key("id tag")</c> pattern and
    /// the <c>mainProduct</c> cycle back through <c>Product</c> resolves
    /// correctly.
    /// </summary>
    [Fact]
    public Task TopProducts_ResolvesNestedCategoryKey() => RunAsync(
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

    /// <summary>
    /// The full audit query. Exercises the nested-list
    /// <c>ProductList @key("products { id pid }")</c> and deeply nested
    /// <c>ProductList @key("products { id pid category { id tag } } selected { id }")</c>
    /// routing through the Fusion planner to populate <c>first</c>,
    /// <c>selected</c>, <c>pid</c>, and <c>price</c> in a single query.
    /// </summary>
    /// <remarks>
    /// Pins that the planner wires each nested-list <c>@key</c> lookup onto the hop
    /// that produces its buried key leaf (<c>pid</c> inside <c>products</c>), so
    /// <c>selected</c>, <c>first</c>, <c>pid</c>, and <c>price</c> all resolve.
    /// </remarks>
    [Fact]
    public Task TopProducts_Projects_Across_All_Subgraphs() => RunAsync(
        query: """
            query {
              topProducts {
                products {
                  id
                  pid
                  price { price }
                  category {
                    mainProduct { id }
                    id
                    tag
                  }
                }
                selected { id }
                first { id }
              }
            }
            """,
        expectedData: """
            {
              "topProducts": {
                "products": [
                  {
                    "id": "1",
                    "pid": "p1",
                    "price": { "price": 100 },
                    "category": {
                      "mainProduct": { "id": "1" },
                      "id": "c1",
                      "tag": "t1"
                    }
                  },
                  {
                    "id": "2",
                    "pid": "p2",
                    "price": { "price": 200 },
                    "category": {
                      "mainProduct": { "id": "2" },
                      "id": "c2",
                      "tag": "t2"
                    }
                  }
                ],
                "selected": { "id": "2" },
                "first": { "id": "1" }
              }
            }
            """);
}
