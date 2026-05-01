using HotChocolate.Fusion.Suites.AbstractTypes.Agency;
using HotChocolate.Fusion.Suites.AbstractTypes.Books;
using HotChocolate.Fusion.Suites.AbstractTypes.Inventory;
using HotChocolate.Fusion.Suites.AbstractTypes.Magazines;
using HotChocolate.Fusion.Suites.AbstractTypes.Products;
using HotChocolate.Fusion.Suites.AbstractTypes.Reviews;
using HotChocolate.Fusion.Suites.AbstractTypes.Users;

namespace HotChocolate.Fusion.Suites;

public sealed class AbstractTypesTests : ComplianceTestBase
{
    private const string SkipAbstractFieldResolution =
        "Planner cannot resolve fields on concrete types through abstract type references across subgraphs.";

    private const string SkipDirectivesOnFragments =
        "Planner does not correctly handle @skip/@include directives on inline fragments with abstract types.";

    private const string SkipUnionTypeFields =
        "Union type fields on interface implementations are not exposed through the composed schema.";
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (AgencySubgraph.Name, AgencySubgraph.BuildAsync),
            (BooksSubgraph.Name, BooksSubgraph.BuildAsync),
            (InventorySubgraph.Name, InventorySubgraph.BuildAsync),
            (MagazinesSubgraph.Name, MagazinesSubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync),
            (ReviewsSubgraph.Name, ReviewsSubgraph.BuildAsync),
            (UsersSubgraph.Name, UsersSubgraph.BuildAsync));

    [Fact]
    public Task Products_With_Dimensions() => RunAsync(
        query: """
            {
              products {
                id
                dimensions { size weight }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "dimensions": { "size": "small", "weight": 0.5 } },
                { "id": "p3", "dimensions": { "size": "small", "weight": 0.6 } },
                { "id": "p2", "dimensions": { "size": "small", "weight": 0.2 } },
                { "id": "p4", "dimensions": { "size": "small", "weight": 0.3 } }
              ]
            }
            """);

    [Fact]
    public Task Similar_With_Sku() => RunAsync(
        query: """
            {
              similar(id: "p1") {
                id
                sku
              }
            }
            """,
        expectedData: """
            {
              "similar": [
                { "id": "p3", "sku": "sku-3" }
              ]
            }
            """);

    [Fact(Skip = SkipAbstractFieldResolution)]
    public Task Similar_Books_And_Magazines_With_Delivery() => RunAsync(
        query: """
            {
              book: similar(id: "p1") {
                id
                delivery(zip: "1234") {
                  estimatedDelivery
                  fastestDelivery
                }
              }
              magazine: similar(id: "p2") {
                id
                delivery(zip: "1234") {
                  estimatedDelivery
                  fastestDelivery
                }
              }
            }
            """,
        expectedData: """
            {
              "book": [
                {
                  "id": "p3",
                  "delivery": {
                    "estimatedDelivery": "1 day",
                    "fastestDelivery": "same day"
                  }
                }
              ],
              "magazine": [
                {
                  "id": "p4",
                  "delivery": {
                    "estimatedDelivery": "1 day",
                    "fastestDelivery": "same day"
                  }
                }
              ]
            }
            """);

    [Fact]
    public Task Products_Sku_With_TypeName_Fragments() => RunAsync(
        query: """
            {
              products {
                id
                sku
                ... on Product { sku }
                ... on Book { sku }
                ... on Magazine { sku }
                ... on Similar { __typename type: __typename }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "sku": "sku-1", "__typename": "Book", "type": "Book" },
                { "id": "p3", "sku": "sku-3", "__typename": "Book", "type": "Book" },
                { "id": "p2", "sku": "sku-2", "__typename": "Magazine", "type": "Magazine" },
                { "id": "p4", "sku": "sku-4", "__typename": "Magazine", "type": "Magazine" }
              ]
            }
            """);

    [Fact]
    public Task Products_Author_With_TotalProductsCreated() => RunAsync(
        query: """
            {
              products {
                author: createdBy {
                  email
                  totalProductsCreated
                }
                ... on Magazine { title }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "author": { "email": "u1@example.com", "totalProductsCreated": 2 } },
                { "author": { "email": "u2@example.com", "totalProductsCreated": 2 } },
                { "author": { "email": "u1@example.com", "totalProductsCreated": 2 }, "title": "Magazine 1" },
                { "author": { "email": "u2@example.com", "totalProductsCreated": 2 }, "title": "Magazine 2" }
              ]
            }
            """);

    [Fact(Skip = SkipAbstractFieldResolution)]
    public Task Products_Reviews_With_Nested_Product_Info() => RunAsync(
        query: """
            {
              products {
                id
                reviews {
                  product {
                    sku
                    ... on Magazine { title }
                    ... on Book { reviewsCount }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "product": { "sku": "sku-1", "reviewsCount": 2 } }, { "product": { "sku": "sku-1", "reviewsCount": 2 } }] },
                { "id": "p3", "reviews": [] },
                { "id": "p2", "reviews": [{ "product": { "sku": "sku-2", "title": "Magazine 1" } }] },
                { "id": "p4", "reviews": [] }
              ]
            }
            """);

    [Fact]
    public Task Products_Reviews_Just_Ids() => RunAsync(
        query: """
            {
              products {
                id
                reviews { id }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }] },
                { "id": "p3", "reviews": [] },
                { "id": "p2", "reviews": [{ "id": 3 }] },
                { "id": "p4", "reviews": [] }
              ]
            }
            """);

    [Fact]
    public Task Products_Reviews_Include_Book_Title_True() => RunAsync(
        query: """
            query ($title: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @include(if: $title) { title }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }], "title": "Book 1" },
                { "id": "p3", "reviews": [], "title": "Book 2" },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact(Skip = SkipDirectivesOnFragments)]
    public Task Products_Reviews_Include_Book_Title_False() => RunAsync(
        query: """
            query ($title: Boolean = false) {
              products {
                id
                reviews { id }
                ... on Book @include(if: $title) { title }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }] },
                { "id": "p3", "reviews": [] },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact]
    public Task Products_Reviews_Skip_Book_Title_False() => RunAsync(
        query: """
            query ($title: Boolean = false) {
              products {
                id
                reviews { id }
                ... on Book @skip(if: $title) { title }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }], "title": "Book 1" },
                { "id": "p3", "reviews": [], "title": "Book 2" },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact(Skip = SkipDirectivesOnFragments)]
    public Task Products_Reviews_Skip_Book_Title_True() => RunAsync(
        query: """
            query ($title: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @skip(if: $title) { title }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }] },
                { "id": "p3", "reviews": [] },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact(Skip = SkipDirectivesOnFragments)]
    public Task Products_Reviews_Skip_Book_Title_True_With_Sku() => RunAsync(
        query: """
            query ($title: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @skip(if: $title) { title }
                ... on Book { sku }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }], "sku": "sku-1" },
                { "id": "p3", "reviews": [], "sku": "sku-3" },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact(Skip = SkipDirectivesOnFragments)]
    public Task Products_Reviews_Skip_Book_Title_False_With_Sku() => RunAsync(
        query: """
            query ($title: Boolean = false) {
              products {
                id
                reviews { id }
                ... on Book @skip(if: $title) { title }
                ... on Book { sku }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }], "title": "Book 1", "sku": "sku-1" },
                { "id": "p3", "reviews": [], "title": "Book 2", "sku": "sku-3" },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact]
    public Task Products_Reviews_Include_Book_Title_Nested_Sku() => RunAsync(
        query: """
            query ($title: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @include(if: $title) {
                  title
                  ... on Book { sku }
                }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }], "title": "Book 1", "sku": "sku-1" },
                { "id": "p3", "reviews": [], "title": "Book 2", "sku": "sku-3" },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact]
    public Task Products_Reviews_Include_Book_Title_Skip_Nested_Sku() => RunAsync(
        query: """
            query ($title: Boolean = true, $sku: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @include(if: $title) {
                  title
                  ... on Book @skip(if: $sku) { sku }
                }
                ... on Magazine { sku }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "reviews": [{ "id": 1 }, { "id": 2 }], "title": "Book 1" },
                { "id": "p3", "reviews": [], "title": "Book 2" },
                { "id": "p2", "reviews": [{ "id": 3 }], "sku": "sku-2" },
                { "id": "p4", "reviews": [], "sku": "sku-4" }
              ]
            }
            """);

    [Fact(Skip = SkipUnionTypeFields)]
    public Task Products_Reviews_PublisherType_Agency_Self() => RunAsync(
        query: """
            {
              products {
                id
                publisherType {
                  ... on Agency { id companyName }
                  ... on Self { email }
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "publisherType": { "email": "u1@example.com" } },
                { "id": "p3", "publisherType": { "id": "a1", "companyName": "Agency 1" } },
                { "id": "p2", "publisherType": { "id": "a1", "companyName": "Agency 1" } },
                { "id": "p4", "publisherType": { "email": "u1@example.com" } }
              ]
            }
            """);

    [Fact(Skip = SkipUnionTypeFields)]
    public Task Products_Reviews_PublisherType_With_Group() => RunAsync(
        query: """
            {
              products {
                id
                publisherType {
                  ... on Agency { id companyName }
                  ... on Self { email }
                  ... on Group { name }
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "publisherType": { "email": "u1@example.com" } },
                { "id": "p3", "publisherType": { "id": "a1", "companyName": "Agency 1" } },
                { "id": "p2", "publisherType": { "id": "a1", "companyName": "Agency 1" } },
                { "id": "p4", "publisherType": { "email": "u1@example.com" } }
              ]
            }
            """);

    [Fact(Skip = SkipUnionTypeFields)]
    public Task Products_Reviews_PublisherType_Email_Aliases() => RunAsync(
        query: """
            {
              products {
                id
                publisherType {
                  ... on Agency { id companyName emailObj: email { address } }
                  ... on Self { emailStr: email }
                  ... on Group { name emailStr: email }
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                { "id": "p1", "publisherType": { "emailStr": "u1@example.com" } },
                { "id": "p3", "publisherType": { "id": "a1", "companyName": "Agency 1", "emailObj": { "address": "a1@example.com" } } },
                { "id": "p2", "publisherType": { "id": "a1", "companyName": "Agency 1", "emailObj": { "address": "a1@example.com" } } },
                { "id": "p4", "publisherType": { "emailStr": "u1@example.com" } }
              ]
            }
            """);
}
