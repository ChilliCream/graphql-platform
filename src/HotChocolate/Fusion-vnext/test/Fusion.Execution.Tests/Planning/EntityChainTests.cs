using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class EntityChainTests : FusionTestBase
{
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
