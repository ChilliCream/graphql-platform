using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementParityTests : FusionTestBase
{
    [Fact(Skip = "WIP: composite-schema fixture translation for deep nested requires.")]
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
              commentById(id: ID!): Comment @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
            }

            type Comment @key(fields: "id") {
              id: ID!
              authorId: ID
              body: String!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              postById(id: ID!): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int!): [Comment]
              author(
                commentAuthorIds: [CommentAuthorIdInput!]!
                  @require(field: "comments { authorId }")): Author
            }

            input CommentAuthorIdInput {
              authorId: ID
            }

            type Comment @key(fields: "id") {
              id: ID!
              authorId: ID
            }

            type Author {
              id: ID!
              name: String
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
}
