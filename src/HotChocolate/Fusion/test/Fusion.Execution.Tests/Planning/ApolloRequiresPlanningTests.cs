using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class ApolloRequiresPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_ResolveFieldFromSecondProvider_When_RequiringSchemaKeyIsNotResolvable()
    {
        // arrange
        var schema = CreateSecondProviderSchema(
            """
            # name: alt
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Product @key(fields: "id") {
              id: ID!
              estimate: Int @shareable
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
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            id: e11e38c8a31230ae819528c27175f16042d592e0b2cff2cd345518e823d7987a
            operation:
              - document: |
                  {
                    products {
                      id
                      id @fusion__requirement
                      estimate
                    }
                  }
                kind: Query
                id: "123456789101112"
                hash: "123456789101112"
                shortHash: "123456789101112"
            searchSpace: 2
            expandedNodes: 4
            nodes:
              - id: 1
                type: Operation
                schema: req
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
                type: ApolloOperation
                schema: alt
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query($representations: [_Any!]!) {
                      _entities(representations: $representations) {
                        ... on Product {
                          estimate
                        }
                      }
                    }
                  hash: 07c096c77edf179ea746d5bf5d8c6ee14831364de1535931b823533f57abb5c0
                  shortHash: 07c096c7
                  xxHash: 4484875409075470930
                entityType: Product
                resultSelectionSet: >-
                  { estimate }
                source: $._entities
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
    public void Plan_Should_ResolveFieldFromSecondProvider_When_SecondProviderAlsoRequiresData()
    {
        // arrange
        var schema = CreateSecondProviderSchema(
            """
            # name: alt
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.3"
                import: ["@key", "@external", "@requires", "@shareable"])

            type Product @key(fields: "id") {
              id: ID!
              weight: Int @external
              estimate: Int @requires(fields: "weight") @shareable
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
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ResolveFieldsFromOtherProviders_When_RequiringSchemaKeyIsNotResolvable()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: req
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.3"
                import: ["@key", "@external", "@requires", "@shareable"])

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id", resolvable: false) {
              id: ID!
              weight: Int @external
              estimate: Int @requires(fields: "weight") @shareable
              quote: Int @requires(fields: "weight") @shareable
            }
            """,
            """
            # name: sup
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              name: String
              weight: Int
            }
            """,
            """
            # name: alt
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Product @key(fields: "id") {
              id: ID!
              estimate: Int @shareable
            }
            """,
            """
            # name: far
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Product @key(fields: "id") {
              id: ID!
              quote: Int @shareable
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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ResolveFieldFromSecondProvider_When_RequiringSchemaAlsoHoldsResolvableEntity()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: req
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.3"
                import: ["@key", "@external", "@requires", "@shareable"])

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id", resolvable: false) {
              id: ID!
              weight: Int @external
              estimate: Int @requires(fields: "weight") @shareable
            }

            type Store @key(fields: "id") {
              id: ID!
              name: String
            }
            """,
            """
            # name: sup
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              name: String
              weight: Int
              store: Store
            }

            type Store @key(fields: "id", resolvable: false) {
              id: ID!
            }
            """,
            """
            # name: alt
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Product @key(fields: "id") {
              id: ID!
              estimate: Int @shareable
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
                store {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ResolveFieldsFromSecondProvider_When_SecondProviderServesTwoRequiringFields()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: req
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.3"
                import: ["@key", "@external", "@requires", "@shareable"])

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID!
              weight: Int @external
              estimate: Int @requires(fields: "weight") @shareable
              quote: Int @requires(fields: "weight") @shareable
            }
            """,
            """
            # name: sup
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              weight: Int
            }
            """,
            """
            # name: alt
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Product @key(fields: "id") {
              id: ID!
              estimate: Int @shareable
              quote: Int @shareable
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
            id: f14434945c6b5ab6159cd28b8375916d2b701b8a8cd127503ed03bf9cf6019e6
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
            expandedNodes: 10
            nodes:
              - id: 1
                type: Operation
                schema: req
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
                type: ApolloOperation
                schema: alt
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query($representations: [_Any!]!) {
                      _entities(representations: $representations) {
                        ... on Product {
                          quote
                          estimate
                        }
                      }
                    }
                  hash: f9a61bcd776242e9024c734551cc5f052cb270f66f338ce301b80ca98437261b
                  shortHash: f9a61bcd
                  xxHash: 4041679236646139285
                entityType: Product
                resultSelectionSet: >-
                  { quote estimate }
                source: $._entities
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
    public void Plan_Should_MakeEstimateWaitForWeight_When_ClientSelectsWeightAndEstimate()
    {
        // arrange
        var schema = CreateRequiresChainSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                name
                dimensions
                weight
                estimate
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_MakeEstimateWaitForWeight_When_ClientSelectsWholeChain()
    {
        // arrange
        var schema = CreateRequiresChainSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                name
                dimensions
                volume
                weight
                estimate
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_MakeEstimateAndTaxWaitForWeight_When_TwoFieldsRequireWeight()
    {
        // arrange
        var schema = CreateRequiresChainSchema(
            """
            # name: tax
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

            type Product @key(fields: "id") {
              id: ID!
              weight: Int @external
              tax: Int @requires(fields: "weight")
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
                tax
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateRequiresChainSchema(params string[] additionalSchemas)
        => ComposeSchema(
        [
            """
            # name: products
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
              dimensions: [Int!]
            }
            """,
            """
            # name: geometry
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

            type Product @key(fields: "id") {
              id: ID!
              dimensions: [Int!] @external
              volume: Int! @requires(fields: "dimensions")
            }
            """,
            """
            # name: metrics
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

            type Product @key(fields: "id") {
              id: ID!
              volume: Int! @external
              weight: Int @requires(fields: "volume")
            }
            """,
            """
            # name: shipping
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

            type Product @key(fields: "id") {
              id: ID!
              weight: Int @external
              estimate: Int @requires(fields: "weight")
            }
            """,
            .. additionalSchemas
        ]);

    private static FusionSchemaDefinition CreateSecondProviderSchema(string secondProvider)
        => ComposeSchema(
            """
            # name: req
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.3"
                import: ["@key", "@external", "@requires", "@shareable"])

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id", resolvable: false) {
              id: ID!
              weight: Int @external
              estimate: Int @requires(fields: "weight") @shareable
            }
            """,
            """
            # name: sup
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              name: String
              weight: Int
            }
            """,
            secondProvider);
}
