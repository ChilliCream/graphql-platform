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
            id: 311b07525b07dd59bffca1dbe05220e9649de3fee39ce93dc2ed0edae8e85c01
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
                kind: Query
                id: "123456789101112"
                hash: "123456789101112"
                shortHash: "123456789101112"
            searchSpace: 1
            expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: ordering
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
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
                  hash: 9b2e2617d620302ef28fe5dc8397958b5ce63585922e867b7a2a2ca9df2f7f52
                  shortHash: 9b2e2617
                  xxHash: 12825943745347590788
                resultSelectionSet: >-
                  { orders }
              - id: 2
                type: Operation
                schema: products
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_1_id: ID!) {
                      productById(id: $__fusion_1_id) {
                        name
                        description
                      }
                    }
                  hash: 7e5b8398096277dabf82b61e5149571619d45e0581424bff89723f8c21c01f9f
                  shortHash: 7e5b8398
                  xxHash: 3351614611179029209
                lookupTypeName: Product
                resultSelectionSet: >-
                  { name description }
                source: $.productById
                target: $.orders<MultiOrderBase>.items.product
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.orders<MultiOrderBase>.items.product
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
            id: 3caf75d8a567918bbf3b6367df82419b253b6458ad0e57294071a00428f0e66f
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
                kind: Query
                id: "123456789101112"
                hash: "123456789101112"
                shortHash: "123456789101112"
            searchSpace: 1
            expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: ordering
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
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
                  hash: fab848d2bae548f01b9c713edfa1b5d025ed3a074a67da598296c85dcff5a245
                  shortHash: fab848d2
                  xxHash: 14528665066443312884
                resultSelectionSet: >-
                  { orders }
              - id: 2
                type: OperationBatch
                schema: products
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_1_id: ID!) {
                      productById(id: $__fusion_1_id) {
                        name
                        description
                      }
                    }
                  hash: 7e5b8398096277dabf82b61e5149571619d45e0581424bff89723f8c21c01f9f
                  shortHash: 7e5b8398
                  xxHash: 3351614611179029209
                lookupTypeName: Product
                resultSelectionSet: >-
                  { name description }
                source: $.productById
                targets:
                  - $.orders<OrderB>.items.product
                  - $.orders<OrderA>.items.product
                batchingGroupId: 4
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.orders<OrderB>.items.product
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }
}
