using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class ProvidesPlannerTests : FusionTestBase
{
    [Fact]
    public void Provides_Partial_Covers_Some()
    {
        // arrange
        var schema = CreatePartialProvidesSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                reviews {
                  author {
                    username
                    email
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Provides_On_Interface()
    {
        // arrange
        var schema = CreateProvidesOnInterfaceSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              book {
                featured {
                  ... on Cat {
                    age
                  }
                  ... on Dog {
                    tricks
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Provides_External_Without_Cover()
    {
        // arrange
        var schema = CreatePartialProvidesSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                reviews {
                  author {
                    email
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Provides_Deeply_Nested_Chain()
    {
        // arrange
        var schema = CreateDeeplyNestedProvidesSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              root {
                level1 {
                  level2 {
                    level3 {
                      value
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
    public void Provides_With_Requires_Interaction()
    {
        // arrange
        var schema = CreateProvidesWithRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              orders {
                item {
                  shippingCost
                }
              }
            }
            """);

        // assert
        // op4 fetches 'weight' from 'orders' via the @provides inlining path, not despite @external.
        MatchSnapshot(plan);
    }

    [Fact]
    public void Planner_Should_Route_To_Owning_Source_When_Local_Field_Is_Orphan_External()
    {
        // arrange
        var schema = CreateOrphanExternalSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              reviews {
                product {
                  name
                }
              }
            }
            """);

        // assert
        // The query enters the 'reviews' source (which owns Query.reviews). 'reviews'
        // also declares Product.name, but as @external with no @provides on the query
        // path referencing it. The partitioner must therefore refuse to resolve 'name'
        // from 'reviews' and route it to 'products' via a productById lookup.
        MatchSnapshot(plan);
    }

    [Fact]
    public void Provides_Shareable_Override()
    {
        // arrange
        var schema = CreateProvidesShareableOverrideSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                reviews {
                  author {
                    displayName
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreatePartialProvidesSchema()
    {
        return ComposeSchema(
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              products: [Product]
            }

            type Product @key(fields: "upc") {
              upc: String!
              name: String
            }
            """,
            """
            # name: reviews
            schema {
              query: Query
            }

            type Query {
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              reviews: [Review]
            }

            type Review @key(fields: "id") {
              id: ID!
              author: User @provides(fields: "username")
            }

            type User @key(fields: "id") {
              id: ID!
              username: String @external
            }
            """,
            """
            # name: users
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              username: String
              email: String
              name: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateProvidesOnInterfaceSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              book: Book
            }

            type Book @key(fields: "id") {
              id: ID!
              featured: Animal @provides(fields: "... on Cat { age } ... on Dog { tricks }")
            }

            interface Animal {
              id: ID!
            }

            type Cat implements Animal @key(fields: "id") {
              id: ID!
              age: Int @external
            }

            type Dog implements Animal @key(fields: "id") {
              id: ID!
              tricks: [String] @external
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              catById(id: ID! @is(field: "id")): Cat @lookup @internal
              dogById(id: ID! @is(field: "id")): Dog @lookup @internal
            }

            interface Animal {
              id: ID!
            }

            type Cat implements Animal @key(fields: "id") {
              id: ID!
              age: Int
            }

            type Dog implements Animal @key(fields: "id") {
              id: ID!
              tricks: [String]
            }
            """);
    }

    private static FusionSchemaDefinition CreateDeeplyNestedProvidesSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              root: Root
            }

            type Root {
              level1: Level1 @provides(fields: "level2 { level3 { value } }")
            }

            type Level1 @key(fields: "id") {
              id: ID!
              level2: Level2 @external
            }

            type Level2 @key(fields: "id") {
              id: ID!
              level3: Level3 @external
            }

            type Level3 @key(fields: "id") {
              id: ID!
              value: String @external
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              level1ById(id: ID! @is(field: "id")): Level1 @lookup @internal
              level2ById(id: ID! @is(field: "id")): Level2 @lookup @internal
              level3ById(id: ID! @is(field: "id")): Level3 @lookup @internal
            }

            type Level1 @key(fields: "id") {
              id: ID!
              level2: Level2
            }

            type Level2 @key(fields: "id") {
              id: ID!
              level3: Level3
            }

            type Level3 @key(fields: "id") {
              id: ID!
              value: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateProvidesWithRequiresSchema()
    {
        return ComposeSchema(
            """
            # name: orders
            schema {
              query: Query
            }

            type Query {
              orders: [Order]
            }

            type Order @key(fields: "id") {
              id: ID!
              item: Item @provides(fields: "weight")
            }

            type Item @key(fields: "sku") {
              sku: String!
              weight: Int @external
            }
            """,
            """
            # name: shipping
            schema {
              query: Query
            }

            type Query {
              itemBySku(sku: String! @is(field: "sku")): Item @lookup @internal
            }

            type Item @key(fields: "sku") {
              sku: String!
              weight: Int
              shippingCost(weight: Int @require(field: "weight")): Float
            }
            """);
    }

    private static FusionSchemaDefinition CreateOrphanExternalSchema()
    {
        return ComposeSchema(
            """
            # name: reviews
            schema {
              query: Query
            }

            type Query {
              reviews: [Review]
              # off-path @provides exists only to satisfy the ExternalUnusedRule;
              # it is never exercised by the query under test.
              productByName(name: String!): Product @provides(fields: "name")
            }

            type Review @key(fields: "id") {
              id: ID!
              body: String
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String @external
            }
            """,
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateProvidesShareableOverrideSchema()
    {
        return ComposeSchema(
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              products: [Product]
            }

            type Product @key(fields: "upc") {
              upc: String!
              reviews: [Review]
            }

            type Review @key(fields: "id") {
              id: ID!
              author: User @provides(fields: "displayName")
            }

            type User @key(fields: "id") {
              id: ID!
              displayName: String @external
            }
            """,
            """
            # name: users
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              displayName: String @shareable
            }
            """,
            """
            # name: accounts
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              displayName: String @shareable
            }
            """);
    }
}
