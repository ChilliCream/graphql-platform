using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementArgumentTests : FusionTestBase
{
    [Fact]
    public void Fed_Audit_Requires_With_Argument_Conflict()
    {
        // arrange
        var schema = CreateRequiresWithArgumentConflictSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                upc
                name
                shippingEstimate
                shippingEstimateEUR
                isExpensiveCategory
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Multiple_Requires_With_Args_That_Conflicts()
    {
        // arrange
        var schema = CreateSimpleRequiresArgsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              test {
                id
                fieldWithRequiresAndArgs
                anotherWithRequiresAndArgs
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Multiple_Plain_Field_And_Requires_With_Args_That_Conflicts()
    {
        // arrange
        var schema = CreateSimpleRequiresArgsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              test {
                id
                fieldWithRequiresAndArgs
                anotherWithRequiresAndArgs
                otherField(arg: 1)
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Multiple_Plain_Field_And_Requires_With_Args_That_Does_Not_Conflicts_Should_Merge()
    {
        // arrange
        var schema = CreateSimpleRequiresArgsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              test {
                id
                fieldWithRequiresAndArgs
                otherField
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Arguments_Deeply_Nested_Requires()
    {
        // arrange
        var schema = CreateAuditRequiresArgumentsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              feed {
                author {
                  id
                }
                comments(limit: 1) {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Arguments_Deeply_Nested_Requires_With_Variable()
    {
        // arrange
        var schema = CreateAuditRequiresArgumentsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($limit: Int = 1) {
              feed {
                author {
                  id
                }
                comments(limit: $limit) {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Arguments_Deeply_Nested_Requires_With_Variables_And_Fragments()
    {
        // arrange
        var schema = CreateAuditRequiresArgumentsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($limit: Int = 1) {
              feed {
                author {
                  id
                }
                ...Foo
                ...Bar
              }
            }

            fragment Foo on Post {
              comments(limit: $limit) {
                id
              }
            }

            fragment Bar on Post {
              comments(limit: $limit) {
                id
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_With_Arguments()
    {
        // arrange
        var schema = CreateRequiresWithArgumentsSchema();

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
    public void Simple_Requires_Arguments()
    {
        // arrange
        var schema = CreateSimpleRequiresArgsSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              test {
                id
                fieldWithRequiresAndArgs
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateSimpleRequiresArgsSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              test: Test
              testById(id: ID! @is(field: "id")): Test @lookup @internal
            }

            type Test @key(fields: "id") {
              id: ID!
              name: String!
              fieldWithRequiresAndArgs(
                otherField: String! @require(field: "otherField")): String!
              fieldWithSameRequiresAndArgs(
                otherField: String! @require(field: "otherField")): String!
              anotherWithRequiresAndArgs(
                otherField: String! @require(field: "otherField")): String!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              testById(id: ID! @is(field: "id")): Test @lookup @internal
            }

            type Test @key(fields: "id") {
              id: ID!
              otherField(arg: Int): String!
            }
            """);
    }

    private static FusionSchemaDefinition CreateAuditRequiresArgumentsSchema()
    {
        return ComposeSchema(
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int): [Comment]
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

    private static FusionSchemaDefinition CreateRequiresWithArgumentsSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int): [Comment]
            }

            type Comment @key(fields: "id") {
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
              author(
                somethingElse: [String]
                  @require(field: "comments[somethingElse]")): Author
            }

            type Author {
              id: ID!
              name: String
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
              somethingElse: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateRequiresWithArgumentConflictSchema()
    {
        return ComposeSchema(
            """
            # name: a
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
              shippingEstimateEUR(
                price: Int @require(field: "priceEur")
                weight: Int @require(field: "weight")): Int
              isExpensiveCategory(
                averagePrice: Int @require(field: "category.averagePrice")): Boolean
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              products: [Product]
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
              categoryById(id: ID! @is(field: "id")): Category @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              name: String
              price: Int
              priceEur: Int
              weight: Int
              category: Category
            }

            type Category @key(fields: "id") {
              id: ID!
              averagePrice: Int
            }
            """);
    }
}
