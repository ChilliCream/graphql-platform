using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class InterfaceLookupPlanningTests : FusionTestBase
{
    [Fact]
    public void Abstract_Customer_Interface_With_Id_Only_Is_Plannable()
    {
        // arrange
        var schema = CreateSplitCustomerSchema();

        // act
        var error = Record.Exception(
            () => PlanOperation(
                schema,
                """
                query {
                  ordersByAgent {
                    edges {
                      node {
                        id
                        customer {
                          __typename
                          id
                        }
                      }
                    }
                  }
                }
                """));

        // assert
        Assert.Null(error);
    }

    [Fact]
    public void Abstract_Customer_Interface_With_Email_Is_Plannable()
    {
        // arrange
        var schema = CreateSplitCustomerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              ordersByAgent {
                edges {
                  node {
                    id
                    customer {
                      __typename
                      id
                      email
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateSplitCustomerSchema()
        => ComposeSchema(
            """
            # name: common
            schema {
              query: Query
            }

            type Query {
              ordersByAgent: OrderConnection
            }

            type OrderConnection {
              edges: [OrderEdge!]
            }

            type OrderEdge {
              node: Order!
            }

            type Order @key(fields: "id") {
              id: ID!
              customer: Customer!
            }

            interface Customer {
              id: ID!
            }

            type ch_Customer implements Customer @key(fields: "id") {
              id: ID!
            }

            type de_Customer implements Customer @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: ch
            schema {
              query: Query
            }

            type Query {
              ch_customerById(id: ID! @is(field: "id")): ch_Customer @lookup @internal
            }

            interface Customer {
              id: ID!
              email: String!
            }

            type ch_Customer implements Customer @key(fields: "id") {
              id: ID!
              email: String!
            }
            """,
            """
            # name: de
            schema {
              query: Query
            }

            type Query {
              de_customerById(id: ID! @is(field: "id")): de_Customer @lookup @internal
            }

            interface Customer {
              id: ID!
              email: String!
            }

            type de_Customer implements Customer @key(fields: "id") {
              id: ID!
              email: String!
            }
            """);
}
