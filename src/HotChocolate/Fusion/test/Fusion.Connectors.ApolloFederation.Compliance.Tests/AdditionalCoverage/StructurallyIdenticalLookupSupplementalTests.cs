using HotChocolate.Execution;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Accounts;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Inventory;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Products;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

namespace HotChocolate.Fusion.AdditionalCoverage;

/// <summary>
/// Structurally identical <c>_entities</c> lookups for different target paths at the
/// same dependency depth are sent as one lookup with one deduplicated representation list.
/// </summary>
public sealed class StructurallyIdenticalLookupSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task ApolloEntityLookups_Should_SendOneDeduplicatedLookup_When_TargetPathsShareLookup()
    {
        // arrange
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            (AccountsSubgraph.Name, AccountsSubgraph.BuildAsync),
            (InventorySubgraph.Name, InventorySubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync),
            (ReviewsSubgraph.Name, ReviewsSubgraph.BuildAsync));

        // act
        // 'products.reviews.product' and 'products.reviews.author.reviews.product'
        // both resolve p1 and p2 from the same reviews fetch
        var result = await gateway.Executor.ExecuteAsync(
            """
            {
              products {
                upc
                reviews {
                  product {
                    name
                    shippingEstimate
                  }
                  author {
                    reviews {
                      product {
                        name
                        shippingEstimate
                      }
                    }
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        AuditAssertions.Assert(
            result.ToJson(),
            expectedDataJson:
            """
            {
              "products": [
                {
                  "upc": "p1",
                  "reviews": [
                    {
                      "product": { "name": "p-name-1", "shippingEstimate": 110 },
                      "author": {
                        "reviews": [
                          { "product": { "name": "p-name-1", "shippingEstimate": 110 } },
                          { "product": { "name": "p-name-2", "shippingEstimate": 440 } }
                        ]
                      }
                    }
                  ]
                },
                {
                  "upc": "p2",
                  "reviews": [
                    {
                      "product": { "name": "p-name-2", "shippingEstimate": 440 },
                      "author": {
                        "reviews": [
                          { "product": { "name": "p-name-1", "shippingEstimate": 110 } },
                          { "product": { "name": "p-name-2", "shippingEstimate": 440 } }
                        ]
                      }
                    }
                  ]
                }
              ]
            }
            """,
            expectsErrors: false);
        capture.Requests.Select(r => $"{r.SubgraphName}: {r.Body}").MatchInlineSnapshot(
            """
            [
              "products: {\"query\":\"query Op_02c69ff7_1 { products { upc } }\"}",
              "reviews: {\"query\":\"query($representations: [_Any!]!) { _entities(representations: $representations) { ... on Product { reviews { product { upc } author { reviews { product { upc } } } } } } }\",\"variables\":{\"representations\":[{\"__typename\":\"Product\",\"upc\":\"p1\"},{\"__typename\":\"Product\",\"upc\":\"p2\"}]}}",
              "products: {\"query\":\"query Op_02c69ff7_Batch_866f9681cfdea748($_0_representations:[_Any!]!,$_1_representations:[_Any!]!){_0__entities:_entities(representations:$_0_representations){...on Product{name price weight}} _1__entities:_entities(representations:$_1_representations){...on Product{name price weight}}}\",\"variables\":{\"_0_representations\":[{\"__typename\":\"Product\",\"upc\":\"p1\"},{\"__typename\":\"Product\",\"upc\":\"p2\"}],\"_1_representations\":[{\"__typename\":\"Product\",\"upc\":\"p1\"},{\"__typename\":\"Product\",\"upc\":\"p2\"}]}}",
              "inventory: {\"query\":\"query Op_02c69ff7_Batch_942324f6a3e34c80($_0_representations:[_Any!]!,$_1_representations:[_Any!]!){_0__entities:_entities(representations:$_0_representations){...on Product{shippingEstimate}} _1__entities:_entities(representations:$_1_representations){...on Product{shippingEstimate}}}\",\"variables\":{\"_0_representations\":[{\"__typename\":\"Product\",\"upc\":\"p1\",\"price\":11,\"weight\":1},{\"__typename\":\"Product\",\"upc\":\"p2\",\"price\":22,\"weight\":2}],\"_1_representations\":[{\"__typename\":\"Product\",\"upc\":\"p1\",\"price\":11,\"weight\":1},{\"__typename\":\"Product\",\"upc\":\"p2\",\"price\":22,\"weight\":2}]}}"
            ]
            """);
    }
}
