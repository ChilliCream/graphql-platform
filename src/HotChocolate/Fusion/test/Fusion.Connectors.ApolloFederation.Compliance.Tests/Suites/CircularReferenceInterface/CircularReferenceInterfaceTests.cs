using HotChocolate.Fusion.Suites.CircularReferenceInterface.A;
using HotChocolate.Fusion.Suites.CircularReferenceInterface.B;

namespace HotChocolate.Fusion.Suites;

public sealed class CircularReferenceInterfaceTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    public Task Product_Should_ReturnNestedTypename_When_CircularSamePriceProduct() => RunAsync(
        query: """
            query {
              product {
                samePriceProduct {
                  ... on Product {
                    samePriceProduct {
                      __typename
                    }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "samePriceProduct": {
                  "samePriceProduct": {
                    "__typename": "Book"
                  }
                }
              }
            }
            """);

    [Fact]
    public Task Product_Should_ReturnFullNested_When_CircularWithIdAndPrice() => RunAsync(
        query: """
            query {
              product {
                __typename
                samePriceProduct {
                  __typename
                  ... on Book {
                    id
                  }
                  samePriceProduct {
                    __typename
                    ... on Book {
                      id
                    }
                  }
                }
                ... on Book {
                  __typename
                  id
                  price
                  samePriceProduct {
                    id
                    price
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "__typename": "Book",
                "samePriceProduct": {
                  "__typename": "Book",
                  "id": "3",
                  "samePriceProduct": {
                    "__typename": "Book",
                    "id": "1"
                  },
                  "price": 10.99
                },
                "id": "1",
                "price": 10.99
              }
            }
            """);
}
