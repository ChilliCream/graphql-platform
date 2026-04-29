using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Accounts;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Inventory;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Products;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>simple-requires-provides</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Four Apollo Federation
/// subgraphs (<c>accounts</c>, <c>products</c>, <c>inventory</c>, <c>reviews</c>)
/// share the <c>User</c> and <c>Product</c> entities. The audit verifies
/// that <c>@requires</c> dependencies on <c>shippingEstimate</c> and
/// <c>shippingEstimateTag</c> route through the entity lookup, and that
/// <c>@provides(fields: "username")</c> on <c>Review.author</c> short-circuits
/// the entity call to <c>accounts</c>.
/// </summary>
public sealed class SimpleRequiresProvidesTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (AccountsSubgraph.Name, AccountsSubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync),
            (InventorySubgraph.Name, InventorySubgraph.BuildAsync),
            (ReviewsSubgraph.Name, ReviewsSubgraph.BuildAsync));

    /// <summary>
    /// Single-subgraph baseline: <c>accounts</c> serves <c>me { id }</c>
    /// directly.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Me_Returns_Id_From_Accounts() => RunAsync(
        query: """
            query {
              me {
                id
              }
            }
            """,
        expectedData: """
            {
              "me": { "id": "u1" }
            }
            """);

    /// <summary>
    /// <c>accounts</c> resolves <c>me { id }</c>; the planner enriches
    /// the user with <c>reviews</c> from the <c>reviews</c> subgraph via
    /// the entity lookup on <c>id</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Me_Reviews_Composes_Across_Accounts_And_Reviews() => RunAsync(
        query: """
            query {
              me {
                id
                reviews {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "me": {
                "id": "u1",
                "reviews": [
                  { "id": "r1" },
                  { "id": "r2" }
                ]
              }
            }
            """);

    /// <summary>
    /// Exercises <c>@provides(fields: "username")</c> on <c>Review.author</c>
    /// (the <c>author { id username }</c> selection should be served inline
    /// from <c>reviews</c>) plus an entity hop into <c>inventory</c> for
    /// <c>product { inStock }</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Me_Reviews_Author_Provides_Username_And_Product_InStock() => RunAsync(
        query: """
            query {
              me {
                reviews {
                  id
                  author {
                    id
                    username
                  }
                  product {
                    inStock
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "me": {
                "reviews": [
                  {
                    "id": "r1",
                    "author": { "id": "u1", "username": "u-username-1" },
                    "product": { "inStock": true }
                  },
                  {
                    "id": "r2",
                    "author": { "id": "u1", "username": "u-username-1" },
                    "product": { "inStock": false }
                  }
                ]
              }
            }
            """);

    /// <summary>
    /// Single-subgraph baseline: <c>products</c> serves <c>products { name }</c>
    /// directly.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Products_Returns_Names_From_Products() => RunAsync(
        query: """
            query {
              products {
                name
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "name": "p-name-1" },
                { "name": "p-name-2" }
              ]
            }
            """);

    /// <summary>
    /// Single-subgraph baseline: <c>products</c> serves <c>products { price }</c>
    /// directly.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Products_Returns_Prices_From_Products() => RunAsync(
        query: """
            query {
              products {
                price
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "price": 11 },
                { "price": 22 }
              ]
            }
            """);

    /// <summary>
    /// <c>products</c> resolves the list and key; <c>inventory</c> contributes
    /// <c>shippingEstimate</c> via <c>@requires(price weight)</c>. The planner
    /// must fetch <c>price</c> and <c>weight</c> from <c>products</c> and
    /// attach them to the entity representation passed to <c>inventory</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Products_ShippingEstimate_Routes_Requires_Through_Inventory() => RunAsync(
        query: """
            query {
              products {
                shippingEstimate
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "shippingEstimate": 110 },
                { "shippingEstimate": 440 }
              ]
            }
            """);

    /// <summary>
    /// Same <c>@requires</c> path as <see cref="Products_ShippingEstimate_Routes_Requires_Through_Inventory"/>
    /// but the client also asks for the dependency fields directly.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Products_ShippingEstimate_With_Weight_And_Price_Selected() => RunAsync(
        query: """
            query {
              products {
                shippingEstimate
                weight
                price
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "shippingEstimate": 110, "weight": 1, "price": 11 },
                { "shippingEstimate": 440, "weight": 2, "price": 22 }
              ]
            }
            """);

    /// <summary>
    /// Deeply nested merge: <c>products -> reviews -> author</c> uses
    /// <c>@provides(fields: "username")</c>; <c>products -> reviews -> product</c>
    /// loops back into <c>products</c> for <c>name</c> and into <c>inventory</c>
    /// for <c>shippingEstimate</c> (which itself uses <c>@requires</c>).
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Products_Reviews_Author_Provides_And_Product_Requires() => RunAsync(
        query: """
            {
              products {
                reviews {
                  id
                  author {
                    username
                  }
                  product {
                    name
                    shippingEstimate
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                {
                  "reviews": [
                    {
                      "id": "r1",
                      "author": { "username": "u-username-1" },
                      "product": { "name": "p-name-1", "shippingEstimate": 110 }
                    }
                  ]
                },
                {
                  "reviews": [
                    {
                      "id": "r2",
                      "author": { "username": "u-username-1" },
                      "product": { "name": "p-name-2", "shippingEstimate": 440 }
                    }
                  ]
                }
              ]
            }
            """);

    /// <summary>
    /// <c>me -> reviews -> product -> reviews</c> double-hops the
    /// <c>reviews</c> subgraph for the inner <c>reviews</c> list using
    /// the <c>Product</c> entity key.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Me_Reviews_Product_Reviews_DoubleHops_Reviews() => RunAsync(
        query: """
            {
              me {
                reviews {
                  product {
                    reviews {
                      id
                    }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "me": {
                "reviews": [
                  { "product": { "reviews": [{ "id": "r1" }] } },
                  { "product": { "reviews": [{ "id": "r2" }] } }
                ]
              }
            }
            """);

    /// <summary>
    /// <c>me -> reviews -> product -> inStock</c> hops <c>accounts</c>,
    /// <c>reviews</c>, and <c>inventory</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Me_Reviews_Product_InStock_Across_Three_Subgraphs() => RunAsync(
        query: """
            query {
              me {
                reviews {
                  product {
                    inStock
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "me": {
                "reviews": [
                  { "product": { "inStock": true } },
                  { "product": { "inStock": false } }
                ]
              }
            }
            """);

    /// <summary>
    /// <c>me -> reviews -> product -> shippingEstimate</c> chains a
    /// four-subgraph requires path: <c>accounts</c> for the user,
    /// <c>reviews</c> for the reviews list, <c>products</c> to project the
    /// <c>price</c> and <c>weight</c> dependencies, and <c>inventory</c> to
    /// run the <c>@requires(price weight)</c> resolver.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Me_Reviews_Product_ShippingEstimate_Routes_Requires() => RunAsync(
        query: """
            query {
              me {
                reviews {
                  product {
                    shippingEstimate
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "me": {
                "reviews": [
                  { "product": { "shippingEstimate": 110 } },
                  { "product": { "shippingEstimate": 440 } }
                ]
              }
            }
            """);

    /// <summary>
    /// <c>me -> reviews -> product</c> reads two <c>@requires(price weight)</c>
    /// fields side by side. The planner should fetch the dependencies once
    /// and serve both downstream resolvers from the same representation.
    /// </summary>
    [Fact(Skip = "Federation transformer's RemoveExternalFields strips the @external 'username' field that the @provides(fields: \"username\") directive on Review.author references, so source-schema validation rejects the composition before any test runs. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Me_Reviews_Product_ShippingEstimate_And_Tag() => RunAsync(
        query: """
            query {
              me {
                reviews {
                  product {
                    shippingEstimate
                    shippingEstimateTag
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "me": {
                "reviews": [
                  { "product": { "shippingEstimate": 110, "shippingEstimateTag": "#p1#110#" } },
                  { "product": { "shippingEstimate": 440, "shippingEstimateTag": "#p2#440#" } }
                ]
              }
            }
            """);
}
