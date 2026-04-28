using HotChocolate.Fusion.Suites.RequiresWithArgument.A;
using HotChocolate.Fusion.Suites.RequiresWithArgument.B;
using HotChocolate.Fusion.Suites.RequiresWithArgument.C;
using HotChocolate.Fusion.Suites.RequiresWithArgument.D;

namespace HotChocolate.Fusion.Suites;

public sealed class RequiresWithArgumentTests : ComplianceTestBase
{
    private const string SkipReason =
        "Composition does not yet support @requires with field arguments "
        + "(e.g. price(currency: \"USD\")). The @require FieldSet parser "
        + "rejects argument syntax in the 'field' value.";

    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync),
            (DSubgraph.Name, DSubgraph.BuildAsync));

    [Fact(Skip = SkipReason)]
    public Task Products_ShippingEstimate_And_IsExpensiveCategory() => RunAsync(
        query: """
            query {
              products {
                upc
                name
                shippingEstimate
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
                  "isExpensiveCategory": false
                },
                {
                  "upc": "p2",
                  "name": "p-name-2",
                  "shippingEstimate": 440,
                  "isExpensiveCategory": true
                }
              ]
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task Feed_With_Author_Ids() => RunAsync(
        query: """
            {
              feed {
                author {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                {
                  "author": {
                    "id": "a2"
                  }
                },
                {
                  "author": {
                    "id": "a1"
                  }
                }
              ]
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task Feed_With_Author_And_Limited_Comments() => RunAsync(
        query: """
            query {
              feed {
                author {
                  id
                }
                comments(limit: 1) {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                {
                  "author": {
                    "id": "a2"
                  },
                  "comments": [
                    {
                      "id": "c1"
                    }
                  ]
                },
                {
                  "author": {
                    "id": "a1"
                  },
                  "comments": [
                    {
                      "id": "c4"
                    }
                  ]
                }
              ]
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task Feed_With_Author_And_Limited_Comments_Variable() => RunAsync(
        query: """
            query ($limit: Int = 1) {
              feed {
                author {
                  id
                }
                comments(limit: $limit) {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                {
                  "author": {
                    "id": "a2"
                  },
                  "comments": [
                    {
                      "id": "c1"
                    }
                  ]
                },
                {
                  "author": {
                    "id": "a1"
                  },
                  "comments": [
                    {
                      "id": "c4"
                    }
                  ]
                }
              ]
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task Feed_With_Author_And_Comments_Fragments() => RunAsync(
        query: """
            query ($limit: Int = 1) {
              feed {
                author {
                  id
                }
                ...Foo
                ...Bar
              }
            }

            fragment Foo on Post {
              comments(limit: $limit) {
                id
              }
            }

            fragment Bar on Post {
              comments(limit: $limit) {
                id
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                {
                  "author": {
                    "id": "a2"
                  },
                  "comments": [
                    {
                      "id": "c1"
                    }
                  ]
                },
                {
                  "author": {
                    "id": "a1"
                  },
                  "comments": [
                    {
                      "id": "c4"
                    }
                  ]
                }
              ]
            }
            """);
}
