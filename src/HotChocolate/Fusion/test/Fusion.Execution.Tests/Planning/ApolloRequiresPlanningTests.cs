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
            operation:
              - document: |
                  {
                    products {
                      id
                      id @fusion__requirement
                      estimate
                    }
                  }
                hash: 123456789101112
                searchSpace: 2
                expandedNodes: 4
            nodes:
              - id: 1
                type: Operation
                schema: req
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                    }
                  }
              - id: 2
                type: ApolloOperation
                schema: alt
                operation: |
                  query($representations: [_Any!]!) {
                    _entities(representations: $representations) {
                      ... on Product {
                        estimate
                      }
                    }
                  }
                source: $._entities
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
                expandedNodes: 10
            nodes:
              - id: 1
                type: Operation
                schema: req
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                    }
                  }
              - id: 2
                type: ApolloOperation
                schema: alt
                operation: |
                  query($representations: [_Any!]!) {
                    _entities(representations: $representations) {
                      ... on Product {
                        quote
                        estimate
                      }
                    }
                  }
                source: $._entities
                target: $.products
                requirements:
                  - name: __fusion_2_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

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
