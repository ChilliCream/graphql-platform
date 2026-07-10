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
}
