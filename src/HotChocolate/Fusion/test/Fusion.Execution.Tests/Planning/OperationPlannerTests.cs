namespace HotChocolate.Fusion.Planning;

public class OperationPlannerTests : FusionTestBase
{
    [Fact]
    public void Plan_Empty_SelectionSet_On_Composite_Field()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              productBySlug(slug: "1") { }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            id: 527e90f72468431a8e796babc659e580a5bcedb51a9c063d73fe5685b5ad13e0
            operation:
              - document: |
                  {
                    productBySlug(slug: "1") {
                      __typename @fusion__empty
                    }
                  }
                kind: Query
                id: "123456789101112"
                hash: "123456789101112"
                shortHash: "123456789101112"
            searchSpace: 1
            expandedNodes: 1
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      productBySlug(slug: "1") {
                        __typename
                      }
                    }
                  hash: 2a311c3bc1a59c1eb33f2d5ed6f4b916a1c4dca6906a04f2ed081b2275ccfe62
                  shortHash: 2a311c3b
                  xxHash: 7753463362280747515
                resultSelectionSet: >-
                  { productBySlug }
            """);
    }

    [Fact]
    public void Plan_Simple_Operation_1_Source_Schema()
    {
        // arrange
        var schema = CreateCompositeSchema();

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Simple_Operation_2_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Simple_Operation_3_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Simple_Lookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              price: Float!
            }
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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Simple_Interface_Lookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProduct: Product
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              name: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              price: Float!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProduct {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Simple_Union_Lookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProduct: Product
              # Just here to satisfy satisfiability as I can't make the union lookup internal...
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              name: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              lookupUnionById(id: ID!): SomeUnion @lookup
            }

            union SomeUnion = Product

            type Product {
              id: ID!
              price: Float!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProduct {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Simple_Requirement()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              region: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }
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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Requirement_That_Cannot_Be_Inlined()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              region: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }
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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Key_Requirement()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") {
              id: ID!
              region: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              sku(region: String! @require(field: "region")): String! @shareable
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productBySku(sku: String!): Product @lookup @internal
            }

            type Product {
              sku: String!
              name: String!
            }
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
        MatchSnapshot(plan);
    }
}
