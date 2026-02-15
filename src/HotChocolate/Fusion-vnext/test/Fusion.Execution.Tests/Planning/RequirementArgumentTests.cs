using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementArgumentTests : FusionTestBase
{
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
}
