using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementParityTests : FusionTestBase
{
    [Fact]
    public void Simplest_Requires()
    {
        // arrange
        var schema = CreateSimplestRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                isExpensive
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Simple_Requires()
    {
        // arrange
        var schema = CreateSimpleRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                shippingEstimate
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Two_Fields_Same_Subgraph_Same_Requirement()
    {
        // arrange
        var schema = CreateSimpleRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                shippingEstimate
                shippingEstimate2
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Two_Same_Service_Calls_With_Args_Conflicts()
    {
        // arrange
        var schema = CreateTwoSameServiceCallsWithArgsConflictsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                isExpensive
                reducedPrice
                price(withDiscount: true)
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Deep_Requires()
    {
        // arrange
        var schema = CreateDeepRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              feed {
                author {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Keys_Mashup()
    {
        // arrange
        var schema = CreateKeysMashupSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              b {
                id
                a {
                  id
                  name
                  nameInB
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_With_Fragments_On_Interfaces()
    {
        // arrange
        var schema = CreateRequiresWithFragmentsOnInterfacesSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              userFromA {
                permissions
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateDeepRequiresSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
            }

            type Post @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int!): [Comment]
            }

            type Comment @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              commentById(id: ID! @is(field: "id")): Comment @lookup @internal
            }

            type Comment @key(fields: "id") {
              id: ID!
              authorId: ID
              body: String!
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              author(
                commentAuthorIds: [ID]
                  @require(field: "comments[authorId]")): Author
            }

            type Author {
              id: ID!
              name: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateSimplestRequiresSchema()
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
              price: Int
            }
            """,
            """
            # name: inventory
            schema {
              query: Query
            }

            type Query {
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              isExpensive(price: Int @require(field: "price")): Boolean
            }
            """);
    }

    private static FusionSchemaDefinition CreateSimpleRequiresSchema()
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
              price: Int
              weight: Int
              name: String
            }
            """,
            """
            # name: inventory
            schema {
              query: Query
            }

            type Query {
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              shippingEstimate(
                price: Int @require(field: "price")
                weight: Int @require(field: "weight")): Int
              shippingEstimate2(
                price: Int @require(field: "price")
                weight: Int @require(field: "weight")): String
            }
            """);
    }

    private static FusionSchemaDefinition CreateTwoSameServiceCallsWithArgsConflictsSchema()
    {
        return ComposeSchema(
            """
            # name: inventory
            schema {
              query: Query
            }

            type Query {
              products: [Product]
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              isExpensive(price: Int @require(field: "price")): Boolean
              reducedPrice(price: Int @require(field: "price")): Boolean
            }
            """,
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              price(withDiscount: Boolean): Int
            }
            """);
    }

    private static FusionSchemaDefinition CreateKeysMashupSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              aById(id: ID!): A @lookup @internal
            }

            type A
              @key(fields: "id")
              @key(fields: "id compositeId { two three }") {
              id: ID!
              compositeId: CompositeID!
              name: String!
            }

            type CompositeID {
              two: ID!
              three: ID!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              b: B
              aByIdAndCompositeId(
                id: ID! @is(field: "id")
                compositeIdTwo: ID! @is(field: "compositeId.two")
                compositeIdThree: ID! @is(field: "compositeId.three")): A @lookup @internal
            }

            type B {
              id: ID!
              a: [A!]!
            }

            type A
              @key(fields: "id", resolvable: false)
              @key(fields: "id compositeId { two three }", resolvable: true) {
              id: ID!
              compositeId: CompositeID!
              nameInB(name: String! @require(field: "name")): String!
            }

            type CompositeID {
              two: ID!
              three: ID!
            }
            """);
    }

    private static FusionSchemaDefinition CreateRequiresWithFragmentsOnInterfacesSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              userFromA: User
            }

            type User @key(fields: "id") {
              id: ID!
              profile: Profile! @shareable
            }

            interface Profile {
              displayName: String!
            }

            interface Account implements Profile {
              displayName: String!
              accountType: String!
            }

            type GuestAccount implements Profile & Account {
              displayName: String! @shareable
              accountType: String! @shareable
              guestToken: String! @shareable
            }

            type AdminAccount implements Profile & Account {
              displayName: String! @shareable
              accountType: String! @shareable
              adminLevel: String! @shareable
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              profile: Profile! @shareable
              permissions(
                displayName: String! @require(field: "profile.displayName")
                accountType: String
                  @require(field:
                    "profile<AdminAccount>.accountType | profile<GuestAccount>.accountType")
                adminLevel: String
                  @require(field: "profile<AdminAccount>.adminLevel")
                guestToken: String
                  @require(field: "profile<GuestAccount>.guestToken")): String!
            }

            interface Profile {
              displayName: String!
            }

            interface Account implements Profile {
              displayName: String!
              accountType: String!
            }

            type GuestAccount implements Profile & Account {
              displayName: String! @shareable
              accountType: String! @shareable
              guestToken: String! @shareable
            }

            type AdminAccount implements Profile & Account {
              displayName: String! @shareable
              accountType: String! @shareable
              adminLevel: String! @shareable
            }
            """);
    }
}
