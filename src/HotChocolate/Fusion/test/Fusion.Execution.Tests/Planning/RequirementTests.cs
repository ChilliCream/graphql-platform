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
            id: 89156cd95236e0a1ecb5c13b6dfc2a9c7bea623049cf3d0907623336868a87e9
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
                kind: Query
                id: "123456789101112"
                hash: "123456789101112"
                shortHash: "123456789101112"
            searchSpace: 4
            expandedNodes: 9
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        id
                      }
                    }
                  hash: f991ee77bf09e4eaf8786949bcc441cd46d16baa24b422a6c1d66a03854fa31c
                  shortHash: f991ee77
                  xxHash: 11674115490493392138
                resultSelectionSet: >-
                  { products }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_2_id: ID!) {
                      productById(id: $__fusion_2_id) {
                        quote
                        estimate
                      }
                    }
                  hash: 7d6f34c83caec172909b7418579f2bbd0d897669ab0b3e55aa84b16bd3f6560f
                  shortHash: 7d6f34c8
                  xxHash: 3122987735805767827
                lookupTypeName: Product
                resultSelectionSet: >-
                  { quote estimate }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_2_id
                    type: ID!
                    path: $.products
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
            id: 64351b59cc89eefc50ef51f60b3ce646940b945b308bea293f9a2533e891e425
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
                kind: Query
                id: "123456789101112"
                hash: "123456789101112"
                shortHash: "123456789101112"
            searchSpace: 1
            expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        details {
                          weight
                        }
                        id
                      }
                    }
                  hash: a8add3c3d78f64952b7c303ee03dc8b102e1ddbd17ea61a5979d00c8662e7115
                  shortHash: a8add3c3
                  xxHash: 9816577427737023502
                resultSelectionSet: >-
                  { products { details { __typename weight } id } }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_1_id: Int!, $__fusion_2_weight: Int!) {
                      productById(id: $__fusion_1_id) {
                        details {
                          shippingEstimate(weight: $__fusion_2_weight)
                        }
                      }
                    }
                  hash: 3a4c7fc66de32fed937320388dac9d10675d20dc21623f22c347a987f5eb4545
                  shortHash: 3a4c7fc6
                  xxHash: 8600007650299864244
                lookupTypeName: Product
                resultSelectionSet: >-
                  { details { shippingEstimate } }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    type: Int!
                    path: $.products
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    type: Int!
                    path: $.products.details
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
            id: 2479825b57b4318238bd0e7cfbb3c573d0b0715a1c5999300a3119244b5dcf2f
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
                kind: Query
                id: "123456789101112"
                hash: "123456789101112"
                shortHash: "123456789101112"
            searchSpace: 1
            expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        details {
                          id
                          weight
                        }
                      }
                    }
                  hash: 98c80f2fd2ba0509366aad67411edb00f8584af4bc1357e227e3fb2e3f7d2090
                  shortHash: 98c80f2f
                  xxHash: 32846845523536012
                resultSelectionSet: >-
                  { products }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_1_id: Int!, $__fusion_2_weight: Int!) {
                      detailsById(id: $__fusion_1_id) {
                        shippingEstimate(weight: $__fusion_2_weight)
                      }
                    }
                  hash: 91e556b57e3fbd0120640c1a90d9cfe24f34cedf0a9cfc700626d48090bb603d
                  shortHash: 91e556b5
                  xxHash: 15692226372106188760
                lookupTypeName: Details
                resultSelectionSet: >-
                  { shippingEstimate }
                source: $.detailsById
                target: $.products.details
                requirements:
                  - name: __fusion_1_id
                    type: Int!
                    path: $.products.details
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    type: Int!
                    path: $.products.details
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
