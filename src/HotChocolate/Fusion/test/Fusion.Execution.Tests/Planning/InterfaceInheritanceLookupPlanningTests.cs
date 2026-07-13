namespace HotChocolate.Fusion.Planning;

public sealed class InterfaceInheritanceLookupPlanningTests : FusionTestBase
{
    // Ordering owns the interface hierarchy OrderBase <- MultiOrderBase <- OrderA/OrderB and a
    // Product stub (id only). Products resolves the remaining Product fields through a lookup.
    private const string OrderingSchema =
        """
        # name: ordering
        schema { query: Query }

        type Query {
          orders: [OrderBase]
        }

        interface OrderBase {
          name: String
        }

        interface MultiOrderBase implements OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderA implements MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderB implements MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderItem {
          product: Product
        }

        type Product @key(fields: "id") {
          id: ID!
        }
        """;

    private const string ProductsSchema =
        """
        # name: products
        schema { query: Query }

        type Query {
          productById(id: ID! @is(field: "id")): Product @lookup @internal
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String
          description: String
        }
        """;

    // https://github.com/ChilliCream/graphql-platform/issues/10045
    // A lookup reached through an intermediate interface fragment must plan its target with the
    // abstract interface type condition (<MultiOrderBase>). The executor is then responsible for
    // matching that condition against the concrete runtime __typename (OrderA/OrderB).
    [Fact]
    public void Plan_Should_TargetAbstractTypeCondition_When_LookupReachedThroughInterfaceFragment_Issue10045()
    {
        // arrange
        var schema = ComposeSchema(OrderingSchema, ProductsSchema);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              orders {
                ... on OrderBase {
                  name
                  ... on MultiOrderBase {
                    items {
                      product { id name description }
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  {
                    orders {
                      __typename @fusion__requirement
                      name
                      ... on MultiOrderBase {
                        items {
                          product {
                            id
                            id @fusion__requirement
                            name
                            description
                          }
                        }
                      }
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: ordering
                operation: |
                  query Op_123456789101112_1 {
                    orders {
                      __typename
                      name
                      ... on MultiOrderBase {
                        __typename
                        items {
                          product {
                            id
                          }
                        }
                      }
                    }
                  }
              - id: 2
                type: Operation
                schema: products
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!) {
                    productById(id: $__fusion_1_id) {
                      name
                      description
                    }
                  }
                source: $.productById
                target: $.orders<MultiOrderBase>.items.product
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    // Counterpart to the interface-fragment plan: concrete-type fragments plan per-type targets
    // (<OrderA>/<OrderB>) that match the runtime __typename exactly, which is why the same query
    // resolves correctly when written with concrete fragments.
    [Fact]
    public void Plan_Should_TargetConcreteTypeConditions_When_LookupReachedThroughConcreteFragments_Issue10045()
    {
        // arrange
        var schema = ComposeSchema(OrderingSchema, ProductsSchema);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              orders {
                ... on OrderBase {
                  name
                  ... on OrderA {
                    items { product { id name description } }
                  }
                  ... on OrderB {
                    items { product { id name description } }
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  {
                    orders {
                      __typename @fusion__requirement
                      name
                      ... on OrderA {
                        items {
                          product {
                            id
                            id @fusion__requirement
                            name
                            description
                          }
                        }
                      }
                      ... on OrderB {
                        items {
                          product {
                            id
                            id @fusion__requirement
                            name
                            description
                          }
                        }
                      }
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: ordering
                operation: |
                  query Op_123456789101112_1 {
                    orders {
                      __typename
                      name
                      ... on OrderA {
                        items {
                          product {
                            id
                          }
                        }
                      }
                      ... on OrderB {
                        items {
                          product {
                            id
                          }
                        }
                      }
                    }
                  }
              - id: 2
                type: OperationBatch
                schema: products
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!) {
                    productById(id: $__fusion_1_id) {
                      name
                      description
                    }
                  }
                source: $.productById
                targets:
                  - $.orders<OrderB>.items.product
                  - $.orders<OrderA>.items.product
                batchingGroupId: 2
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }
}
