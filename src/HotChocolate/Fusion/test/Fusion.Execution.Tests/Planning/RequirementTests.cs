using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

public class RequirementTests : FusionTestBase
{
    [Fact]
    public void Plan_Simple_Operation_1_Source_Schema()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              books: [Book]
            }

            type Book @key(fields: "id") {
              id: String!
              title: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              bookById(id: String!): Book @lookup @internal
            }

            type Book {
              id: String!
              titleAndId(title: String @require(field: "title")): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
                books {
                  titleAndId
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requirement_Merged_Into_SelectionSet_With_Non_Lead_Field()
    {
        // arrange
        var schema = ComposeSchema(
            """"
            schema {
              query: Query
            }

            type Brand {
              id: Int!
              name: String!
            }

            type Product @key(fields: "id") {
              id: Int!
              name: String!
              brand: Brand
            }

            type Query {
              products: [Product]
            }
            """",
            """"
            schema {
              query: Query
            }

            type Product {
              nameAndId(name: String! @require(field: "name")): String!
              id: Int!
            }

            type Query {
              productById(id: Int!): Product @lookup @internal
            }
            """");

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                brand { name }
                nameAndId
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requirement_SelectionMap_Object()
    {
        // arrange
        var schema = ComposeSchema(
            """"
            schema {
              query: Query
            }

            type Brand {
              id: Int!
              name: String!
            }

            type Product @key(fields: "id") {
              id: Int!
              name: String!
              brand: Brand
            }

            type Query {
              products: [Product]
            }
            """",
            """"
            schema {
              query: Query
            }

            type Product {
              nameAndId(
                input: NameInput
                  @require(field:
                    """
                    {
                      name
                      brandName: brand.name
                    }
                    """)): String!
              id: Int!
            }

            type Query {
              productById(id: Int!): Product @lookup @internal
            }

            input NameInput {
              name: String!
              brandName: String
            }
            """");

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                brand { name }
                nameAndId
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requirement_SelectionMap_Object_Shop()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              users {
                nodes {
                  reviews {
                    nodes {
                      product {
                        deliveryEstimate(zip: "123")
                      }
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Complex_Operation()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query getReviews($userId: ID!) {
              userById(id: $userId) {
                name
              }
              users {
                nodes {
                  id
                  reviews {
                    nodes {
                      product {
                        deliveryEstimate(zip: "123")
                      }
                    }
                  }
                }
              }
              node(id: $userId) {
                id
                ... on User {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requirement_Directive_Leaks_Into_SourceSchema_Request_Shop()
    {
        // assert
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query findMe($skip: Boolean = true) {
              users {
                nodes {
                  id
                  birthdate
                  reviews {
                    nodes {
                      author {
                        id
                        name
                      }
                      product {
                        id

                        weight
                        deliveryEstimate(zip: "4383")
                        pictureFileName
                        price

                        quantity @skip(if: $skip)

                        reviews {
                          nodes {
                            author {
                              birthdate
                              reviews {
                                nodes {
                                  stars
                                  product {
                                    weight
                                    item {
                                       quantity
                                       product {
                                        name
                                       }
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ResolveFieldsFromSecondSchema_When_SecondSchemaServesTwoRequiringFields()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              products: [Product]
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              estimate(weight: Int @require(field: "weight")): Int @shareable
              quote(weight: Int @require(field: "weight")): Int @shareable
            }
            """,
            """
            # name: b
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              estimate: Int @shareable
              quote: Int @shareable
            }
            """,
            """
            # name: c
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              weight: Int
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                estimate
                quote
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
                    products {
                      id
                      id @fusion__requirement
                      estimate
                      quote
                    }
                  }
                hash: 123456789101112
                searchSpace: 4
                expandedNodes: 9
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2($__fusion_2_id: ID!) {
                    productById(id: $__fusion_2_id) {
                      quote
                      estimate
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_2_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Should_MakeEstimateWaitForWeight_When_ClientSelectsWeightOfThreeHopRequireChain()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: p
            type Query {
              products: [Product]
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
              dimensions: [Int!]
            }
            """,
            """
            # name: g
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              volume(dimensions: [Int!] @require(field: "dimensions")): Int!
            }
            """,
            """
            # name: m
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              weight(volume: Int @require(field: "volume")): Int
            }
            """,
            """
            # name: s
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              estimate(weight: Int @require(field: "weight")): Int
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                weight
                estimate
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_AnchorRequirementBelowLookupTarget_When_RequiringTypeHasNoLookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              products: [Product]
            }

            type Product {
              id: Int! @shareable
              details: Details @shareable
            }

            type Details {
              weight: Int!
            }
            """,
            """
            # name: b
            type Query {
              productById(id: Int!): Product @lookup @internal
            }

            type Product {
              id: Int! @shareable
              details: Details @shareable
            }

            type Details {
              shippingEstimate(weight: Int! @require(field: "weight")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                details {
                  shippingEstimate
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
                    products {
                      details {
                        shippingEstimate
                        weight @fusion__requirement
                      }
                      id @fusion__requirement
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      details {
                        weight
                      }
                      id
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: Int!, $__fusion_2_weight: Int!) {
                    productById(id: $__fusion_1_id) {
                      details {
                        shippingEstimate(weight: $__fusion_2_weight)
                      }
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    selectionMap: >-
                      weight
                dependencies:
                  - id: 1
            """);
        DescribeRequirements(plan).MatchInlineSnapshot(
            """
            2: target $.products, __fusion_1_id at $.products maps id
            2: target $.products, __fusion_2_weight at $.products.details maps weight
            """);
    }

    [Fact]
    public void Plan_Should_AnchorRequirementAtLookupTarget_When_RequiringTypeHasLookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              products: [Product]
            }

            type Product {
              id: Int! @shareable
              details: Details @shareable
            }

            type Details {
              id: Int! @shareable
              weight: Int!
            }
            """,
            """
            # name: b
            type Query {
              productById(id: Int!): Product @lookup @internal
              detailsById(id: Int!): Details @lookup @internal
            }

            type Product {
              id: Int! @shareable
              details: Details @shareable
            }

            type Details {
              id: Int! @shareable
              shippingEstimate(weight: Int! @require(field: "weight")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                details {
                  shippingEstimate
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
                    products {
                      details {
                        shippingEstimate
                        id @fusion__requirement
                        weight @fusion__requirement
                      }
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      details {
                        id
                        weight
                      }
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: Int!, $__fusion_2_weight: Int!) {
                    detailsById(id: $__fusion_1_id) {
                      shippingEstimate(weight: $__fusion_2_weight)
                    }
                  }
                source: $.detailsById
                target: $.products.details
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    selectionMap: >-
                      weight
                dependencies:
                  - id: 1
            """);
        DescribeRequirements(plan).MatchInlineSnapshot(
            """
            2: target $.products.details, __fusion_1_id at $.products.details maps id
            2: target $.products.details, __fusion_2_weight at $.products.details maps weight
            """);
    }

    private static string DescribeRequirements(OperationPlan plan)
    {
        var lines = new List<string>();

        foreach (var node in plan.AllNodes)
        {
            if (node is not OperationExecutionNode operation)
            {
                continue;
            }

            foreach (var requirement in operation.Requirements)
            {
                lines.Add(
                    $"{operation.Id}: target {operation.Target}, "
                    + $"{requirement.Key} at {requirement.Path} maps {requirement.Map}");
            }
        }

        return string.Join('\n', lines);
    }
}
