using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.Planning;

public sealed class UnreachableSourceFieldPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_ResolveEntityFieldThroughLookup_When_EntityKeyIsResolvable()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: A
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              topReviewer: String
            }

            type Product @key(fields: "id") {
              id: ID!
              reviewCount: Int!
            }
            """,
            ProductsSchema);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                name
                reviewCount
              }
              topReviewer
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ResolveFieldsFromOtherSchema_When_UnreachableNonResolvableEntityDeclaresOnlyKeyFields()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: A
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              topReviewer: String
            }

            type Product @key(fields: "id", resolvable: false) {
              id: ID!
            }
            """,
            ProductsSchema);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                name
              }
              topReviewer
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
                      name
                    }
                    topReviewer
                  }
                hash: 123456789101112
                searchSpace: 2
                expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: B
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                      name
                    }
                  }
              - id: 2
                type: Operation
                schema: A
                operation: |
                  query Op_123456789101112_2 {
                    topReviewer
                  }
            """);
    }

    [Fact]
    public void Plan_Should_ResolveRootFields_When_NonResolvableEntityIsReachableOnlyThroughInaccessibleField()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: A
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              topReviewer: String
            }

            type Product @key(fields: "id", resolvable: false) {
              id: ID!
              reviewCount: Int!
            }
            """,
            """
            # name: B
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@inaccessible"])

            type Query {
              products: [Product!]! @inaccessible
              productCount: Int!
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              productCount
              topReviewer
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  {
                    productCount
                    topReviewer
                  }
                hash: 123456789101112
                searchSpace: 2
                expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: A
                operation: |
                  query Op_123456789101112_1 {
                    topReviewer
                  }
              - id: 2
                type: Operation
                schema: B
                operation: |
                  query Op_123456789101112_2 {
                    productCount
                  }
            """);
    }

    [Fact]
    public void Plan_Should_ResolveFieldThroughLookup_When_SchemaKeysEntityThatOnlyAnotherSchemaReturns()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: catalog
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Author @key(fields: "id") {
              id: ID!
              name: String!
            }

            type Book @key(fields: "id") @shareable {
              id: ID!
              shelves: [Shelf!]!
            }

            type Shelf @key(fields: "id") @shareable {
              id: ID!
              books: [Book!]!
            }

            type Query @shareable {
              books: [Book!]!
            }
            """,
            """
            # name: authors
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Author @key(fields: "id") {
              id: ID!
            }

            type Book @key(fields: "id") @shareable {
              id: ID!
              author: Author!
            }

            type Shelf @key(fields: "id") @shareable {
              id: ID!
              books: [Book!]!
            }

            type Query @shareable {
              books: [Book!]!
            }
            """,
            """
            # name: pricing
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Book @key(fields: "id") @shareable {
              id: ID!
              price: Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              books {
                id
                price
                author {
                  id
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ResolveFieldThroughLookup_When_SchemaKeysTypeThatOnlyAnotherSchemaReturns()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: catalog
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query @shareable {
              products: [Product!]!
            }

            type Product @key(fields: "id") @shareable {
              id: ID!
              sellers: [Seller!]!
            }

            type Seller @key(fields: "id") @shareable {
              id: ID!
              products: [Product!]!
            }

            type Warehouse @key(fields: "id") {
              id: ID!
              address: String!
            }
            """,
            """
            # name: inventory
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query @shareable {
              products: [Product!]!
            }

            type Product @key(fields: "id") @shareable {
              id: ID!
              warehouse: Warehouse!
            }

            type Seller @key(fields: "id") @shareable {
              id: ID!
              products: [Product!]!
            }

            type Warehouse @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: pricing
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Product @key(fields: "id") @shareable {
              id: ID!
              price: Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                price
                warehouse {
                  id
                  address
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ResolveExtensionFieldThroughLookup_When_FederationV1ExtensionDeclaresKey()
    {
        // arrange
        var schema = ComposeSchema(
            options =>
            {
                options.Merger.EnableGlobalObjectIdentification = true;
                options.SourceSchemas["authors"] = new SourceSchemaOptions { IsApolloFederationV1 = true };
                options.SourceSchemas["awards"] = new SourceSchemaOptions { IsApolloFederationV1 = true };
            },
            """
            # name: authors
            type Query {
              authors: [Author]
            }

            type Author @key(fields: "id") {
              id: String!
              name: String!
            }
            """,
            """
            # name: awards
            extend type Author @key(fields: "id") {
              id: String! @external
              awards: [Award]
            }

            type Award {
              id: String!
              title: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              authors {
                id
                name
                awards {
                  id
                  title
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private const string ProductsSchema =
        """
        # name: B
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Query {
          products: [Product!]!
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String!
        }
        """;
}
