namespace HotChocolate.Fusion.Planning;

public class OperationPlannerTests : FusionTestBase
{
    [Fact]
    public void Plan_Simple_Operation_1_Source_Schema()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: PRODUCTS
                operation: >-
                  {
                      productBySlug(slug: "1") {
                      id
                      name
                      }
                  }
            """);
    }

    [Fact]
    public void Plan_Simple_Operation_2_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
                estimatedDelivery(postCode: "12345")
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: PRODUCTS
                operation: >-
                  {
                    productBySlug(slug: "1") {
                      id
                      name
                      dimension {
                        height
                        width
                      }
                    }
                  }
              - id: 2
                schema: SHIPPING
                operation: >-
                  {
                    productById(id: $__fusion_1_id) {
                      estimatedDelivery(postCode: "12345", height: $__fusion_2_height, width: $__fusion_2_width)
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: productBySlug
                    selectionMap: id
                  - name: __fusion_2_height
                    selectionSet: productBySlug
                    selectionMap: dimension.height
                  - name: __fusion_2_width
                    selectionSet: productBySlug
                    selectionMap: dimension.width
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Simple_Operation_3_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    ... ProductCard
                }
            }

            fragment ProductCard on Product {
                name
                reviews(first: 10) {
                    nodes {
                        ... ReviewCard
                    }
                }
            }

            fragment ReviewCard on Review {
                body
                stars
                author {
                    ... AuthorCard
                }
            }

            fragment AuthorCard on UserProfile {
                displayName
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: PRODUCTS
                operation: >-
                  {
                    productBySlug(slug: "1") {
                      name
                      id
                    }
                  }
              - id: 2
                schema: REVIEWS
                operation: >-
                  {
                    productById(id: $__fusion_1_id) {
                      reviews(first: 10) {
                        nodes {
                          body
                          stars
                          author {
                            id
                          }
                        }
                      }
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: productBySlug
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: ACCOUNTS
                operation: >-
                  {
                    userById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: productBySlug.author.nodes.reviews
                    selectionMap: id
                dependencies:
                  - id: 2
            """);
    }

    [Fact(Skip = "Fix satisfiability (consider using @inaccessible on some of the lookup fields)")]
    public void Plan_Simple_Lookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product {
              id: ID!
              name: String!
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              price: Float!
            }

            directive @lookup on FIELD_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: A
              operation: >-
              query GetTopProducts_1 {
                topProducts {
                  id
                  name
                }
              }
            - id: 2
              schema: B
              operation: >-
              query GetTopProducts_2 {
                productById(id: $__fusion_1_id) {
                  price
                }
              }
              requirements:
                - name: __fusion_1_id
                  selectionSet: topProducts
                  selectionMap: id
              dependencies:
                - id: 1
            """);
    }

    [Fact(Skip = "Fix satisfiability (consider using @inaccessible on some of the lookup fields)")]
    public void Plan_Simple_Requirement()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product {
              id: ID!
              name: String!
              region: String!
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }

            directive @lookup on FIELD_DEFINITION

            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """);

        // assert
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: A
                operation: >-
                  query GetTopProducts_1 {
                    topProducts {
                      id
                      name
                      region
                    }
                  }
              - id: 2
                schema: B
                operation: >-
                  query GetTopProducts_2 {
                    productById(id: $__fusion_1_id) {
                      price(region: $__fusion_2_region)
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: topProducts
                    selectionMap: id
                  - name: __fusion_2_region
                    selectionSet: topProducts
                    selectionMap: region
                dependencies:
                  - id: 1
            """);
    }

    [Fact(Skip = "Fix satisfiability (consider using @inaccessible on some of the lookup fields)")]
    public void Plan_Requirement_That_Cannot_Be_Inlined()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product {
              id: ID!
              name: String!
              region: String!
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }

            directive @lookup on FIELD_DEFINITION

            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """);

        // assert
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: A
                operation: >-
                  query GetTopProducts_1 {
                    topProducts {
                      id
                      name
                      region
                    }
                  }
              - id: 2
                schema: B
                operation: >-
                  query GetTopProducts_2 {
                    productById(id: $__fusion_1_id) {
                      price(region: $__fusion_2_region)
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: topProducts
                    selectionMap: id
                  - name: __fusion_2_region
                    selectionSet: topProducts
                    selectionMap: region
                dependencies:
                  - id: 1
            """);
    }

    [Fact(Skip = "Fix satisfiability (consider using @inaccessible on some of the lookup fields)")]
    public void Plan_Key_Requirement()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product {
              id: ID!
              region: String!
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              sku(region: String! @require(field: "region")): String!
            }

            directive @lookup on FIELD_DEFINITION

            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "C") {
              query: Query
            }

            type Query {
              productBySku(sku: String!): Product @lookup
            }

            type Product {
              sku: String!
              name: String!
            }

            directive @lookup on FIELD_DEFINITION

            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """);

        // assert
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProducts {
                id
                name
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: A
                operation: >-
                  query GetTopProducts_1 {
                    topProducts {
                      id
                      region
                    }
                  }
              - id: 2
                schema: C
                operation: >-
                  query GetTopProducts_2 {
                    productBySku(sku: $__fusion_1_sku) {
                      name
                    }
                  }
                requirements:
                  - name: __fusion_1_sku
                    selectionSet: topProducts
                    selectionMap: sku
                dependencies:
                  - id: 3
              - id: 3
                schema: B
                operation: >-
                  query GetTopProducts_3 {
                    productById(id: $__fusion_2_id) {
                      sku(region: $__fusion_3_region)
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: topProducts
                    selectionMap: id
                  - name: __fusion_3_region
                    selectionSet: topProducts
                    selectionMap: region
                dependencies:
                  - id: 1
            """);
    }
}
