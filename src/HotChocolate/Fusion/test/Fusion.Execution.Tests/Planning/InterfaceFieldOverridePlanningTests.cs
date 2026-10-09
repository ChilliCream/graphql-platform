using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Pins the output contract of the interface-field ownership divergence expansion in the
/// operation planner: an interface-level field whose ownership diverges across concrete types
/// (for example after an <c>@override</c>) is expanded into per-concrete inline fragments, while
/// a non-diverging field selected alongside it (<c>id</c>) stays untouched at the interface level.
/// The resulting plan routes each concrete branch to its owner.
/// </summary>
public sealed class InterfaceFieldOverridePlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_Spill_Interface_Field_When_Concrete_Ownership_Diverges()
    {
        // arrange
        // The Post interface declares createdAt in schema "a", but the only concrete type that
        // implements Post in "a" (ImagePost) moved createdAt to schema "b" via @override. A field
        // selected on the Post interface must route to b instead of resolving the stale value in a.
        var schema = CreateSingleOwnerOverrideSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              feed {
                id
                createdAt
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Keep_Owning_Branch_And_Route_Only_Diverged_Branch()
    {
        // arrange
        // ImagePost still owns createdAt in schema "a" while TextPost moved createdAt to "b" via
        // @override. Only the diverged TextPost branch must spill to b; the ImagePost branch stays
        // resolvable in a's root fetch.
        var schema = CreateMixedOwnerOverrideSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              feed {
                id
                createdAt
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateSingleOwnerOverrideSchema()
        => ComposeSchema(
            """
            # name: a
            schema { query: Query }

            type Query {
              feed: [Post]
              imagePostById(id: ID! @is(field: "id")): ImagePost @lookup @internal
            }

            interface Post {
              id: ID!
              createdAt: String!
            }

            type ImagePost implements Post @key(fields: "id") {
              id: ID!
              createdAt: String!
            }
            """,
            """
            # name: b
            schema { query: Query }

            type Query {
              anotherFeed: [AnotherPost]
              imagePostById(id: ID! @is(field: "id")): ImagePost @lookup @internal
              textPostById(id: ID! @is(field: "id")): TextPost @lookup @internal
            }

            interface Post {
              id: ID!
              createdAt: String!
            }

            type TextPost implements Post @key(fields: "id") {
              id: ID!
              createdAt: String!
              body: String!
            }

            interface AnotherPost {
              id: ID!
              createdAt: String!
            }

            type ImagePost implements AnotherPost @key(fields: "id") {
              id: ID!
              createdAt: String! @override(from: "a")
            }
            """);

    private static FusionSchemaDefinition CreateMixedOwnerOverrideSchema()
        => ComposeSchema(
            """
            # name: a
            schema { query: Query }

            type Query {
              feed: [Post]
              imagePostById(id: ID! @is(field: "id")): ImagePost @lookup @internal
              textPostById(id: ID! @is(field: "id")): TextPost @lookup @internal
            }

            interface Post {
              id: ID!
              createdAt: String!
            }

            type ImagePost implements Post @key(fields: "id") {
              id: ID!
              createdAt: String!
            }

            type TextPost implements Post @key(fields: "id") {
              id: ID!
              createdAt: String!
            }
            """,
            """
            # name: b
            schema { query: Query }

            type Query {
              textPostById(id: ID! @is(field: "id")): TextPost @lookup @internal
            }

            interface Post {
              id: ID!
              createdAt: String!
            }

            type TextPost implements Post @key(fields: "id") {
              id: ID!
              createdAt: String! @override(from: "a")
            }
            """);
}
