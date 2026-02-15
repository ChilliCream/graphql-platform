using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class EntityChainTests : FusionTestBase
{
    [Fact]
    public void Complex_Entity_Call()
    {
        // arrange
        var schema = CreateComplexEntityCallSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              topProducts {
                products {
                  id
                  price {
                    price
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Parent_Entity_Call_Complex()
    {
        // arrange
        var schema = CreateParentEntityCallComplexSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              productFromD(id: "1") {
                id
                name
                category {
                  id
                  name
                  details
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateComplexEntityCallSchema()
    {
        return ComposeSchema(
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              topProducts: ProductList!
            }

            type ProductList {
              products: [Product!]!
            }

            type Product @key(fields: "id") {
              id: ID!
              category: Category! @shareable
            }

            type Category {
              id: ID! @shareable
              tag: String @shareable
            }
            """,
            """
            # name: link
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              pid: ID! @shareable
            }
            """,
            """
            # name: price
            schema {
              query: Query
            }

            type Query {
              productByIdPidAndCategory(
                id: ID! @is(field: "id")
                pid: ID! @is(field: "pid")
                categoryId: ID! @is(field: "category.id")
                categoryTag: String @is(field: "category.tag")): Product @lookup @internal
            }

            type Product @key(fields: "id pid category { id tag }") {
              id: ID!
              pid: ID! @shareable
              category: Category! @shareable
              price: Price
            }

            type Category {
              id: ID! @shareable
              tag: String @shareable
            }

            type Price {
              price: Float!
            }
            """);
    }

    private static FusionSchemaDefinition CreateParentEntityCallComplexSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              category: Category @shareable
            }

            type Category {
              details: String
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              category: Category @shareable
            }

            type Category @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              categoryById(id: ID! @is(field: "id")): Category @lookup @internal
            }

            type Category @key(fields: "id") {
              id: ID!
              name: String
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              productFromD(id: ID!): Product
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }
            """);
    }
}
