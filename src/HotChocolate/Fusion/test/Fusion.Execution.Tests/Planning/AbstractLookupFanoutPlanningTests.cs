using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class AbstractLookupFanoutPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_Keep_SiblingBranch_Clean_When_Duplicate_TypeFragments_Present()
    {
        // arrange
        // Two `... on Book` fragments on products must not root the Book key requirement into the
        // sibling Magazine-targeted reviews lookup, which would emit an invalid cross-type fragment.
        var schema = CreateProductTitleSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($title: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @skip(if: $title) { title }
                ... on Book { sku }
                ... on Magazine { sku }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Prune_Impossible_Concrete_Fragment_When_Parent_Is_Object()
    {
        // arrange
        // The nested Product selection is partitioned into concrete Book and Magazine branches.
        // A Magazine-only title lookup must not add its id requirement to the sibling Book lookup.
        var schema = CreateNestedProductReviewSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
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
            """);

        // assert
        MatchSnapshot(plan);
    }

    // sku is co-located with the products root in "a", reviews in "r", Book-only title in "books".
    private static FusionSchemaDefinition CreateProductTitleSchema()
        => ComposeSchema(
            """
            # name: a
            schema { query: Query }

            type Query {
              products: [Product]
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! sku: String }
            type Book implements Product @key(fields: "id") { id: ID! sku: String }
            type Magazine implements Product @key(fields: "id") { id: ID! sku: String }
            """,
            """
            # name: r
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! reviews: [Review] }
            type Book implements Product @key(fields: "id") { id: ID! reviews: [Review] }
            type Magazine implements Product @key(fields: "id") { id: ID! reviews: [Review] }
            type Review { id: ID! }
            """,
            """
            # name: books
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
            }

            type Book @key(fields: "id") { id: ID! title: String }
            """);

    private static FusionSchemaDefinition CreateNestedProductReviewSchema()
        => ComposeSchema(
            """
            # name: products
            schema { query: Query }

            type Query {
              products: [Product]
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! sku: String }
            type Book implements Product @key(fields: "id") { id: ID! sku: String }
            type Magazine implements Product @key(fields: "id") { id: ID! sku: String }
            """,
            """
            # name: reviews
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! reviews: [Review] reviewsCount: Int }
            type Book implements Product @key(fields: "id") {
              id: ID!
              reviews: [Review]
              reviewsCount: Int
            }
            type Magazine implements Product @key(fields: "id") {
              id: ID!
              reviews: [Review]
              reviewsCount: Int
            }
            type Review { product: Product }
            """,
            """
            # name: magazines
            schema { query: Query }

            type Query {
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            type Magazine @key(fields: "id") { id: ID! title: String }
            """);
}
