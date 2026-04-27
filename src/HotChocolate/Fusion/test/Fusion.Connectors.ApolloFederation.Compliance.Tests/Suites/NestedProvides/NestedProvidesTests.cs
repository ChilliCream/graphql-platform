using HotChocolate.Fusion.Suites.NestedProvides.AllProducts;
using HotChocolate.Fusion.Suites.NestedProvides.Category;
using HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>nested-provides</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three subgraphs
/// (<c>all-products</c>, <c>category</c>, <c>subcategories</c>) verify
/// that <c>@provides</c> works with nested field selections including
/// subcategories of categories.
/// </summary>
public sealed class NestedProvidesTests : ComplianceTestBase
{
    // The composition validator (SelectionSetValidator) does not unwrap list
    // types when checking whether a field returns a composite type. The
    // @provides selection "categories { id name subCategories { id name } }"
    // traverses the "categories" field which returns [Category]. Because
    // NullableType() strips only non-null wrappers, the validator sees a
    // ListType instead of an IComplexTypeDefinition and rejects the
    // provides selection. All tests in this suite are blocked by this gap
    // in Fusion.Utilities SelectionSetValidator (PROVIDES_INVALID_FIELDS).

    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (AllProductsSubgraph.Name, AllProductsSubgraph.BuildAsync),
            (CategorySubgraph.Name, CategorySubgraph.BuildAsync),
            (SubcategoriesSubgraph.Name, SubcategoriesSubgraph.BuildAsync));

    [Fact(Skip = "SelectionSetValidator does not unwrap list types in @provides field selections (PROVIDES_INVALID_FIELDS).")]
    public Task Products_Categories_With_Names() => RunAsync(
        query: """
            query {
              products {
                id
                categories {
                  id
                  name
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                {
                  "id": "p1",
                  "categories": [
                    { "id": "c1", "name": "Category 1" },
                    { "id": "c2", "name": "Category 2" }
                  ]
                },
                {
                  "id": "p2",
                  "categories": [
                    { "id": "c3", "name": "Category 3" },
                    { "id": "c2", "name": "Category 2" }
                  ]
                }
              ]
            }
            """);

    [Fact(Skip = "SelectionSetValidator does not unwrap list types in @provides field selections (PROVIDES_INVALID_FIELDS).")]
    public Task Products_Categories_With_Names_And_SubCategories() => RunAsync(
        query: """
            query {
              products {
                id
                categories {
                  id
                  name
                  subCategories {
                    id
                    name
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "products": [
                {
                  "id": "p1",
                  "categories": [
                    {
                      "id": "c1",
                      "name": "Category 1",
                      "subCategories": [
                        { "id": "c2", "name": "Category 2" }
                      ]
                    },
                    {
                      "id": "c2",
                      "name": "Category 2",
                      "subCategories": [
                        { "id": "c3", "name": "Category 3" }
                      ]
                    }
                  ]
                },
                {
                  "id": "p2",
                  "categories": [
                    {
                      "id": "c3",
                      "name": "Category 3",
                      "subCategories": [
                        { "id": "c1", "name": "Category 1" }
                      ]
                    },
                    {
                      "id": "c2",
                      "name": "Category 2",
                      "subCategories": [
                        { "id": "c3", "name": "Category 3" }
                      ]
                    }
                  ]
                }
              ]
            }
            """);
}
