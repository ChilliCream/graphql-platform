using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementCrossEntityTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_Resolve_ByNovice_When_Require_Crosses_Entity_Boundary()
    {
        // arrange
        var schema = CreateCircularCrossProviderSchema();

        // act
        // b.byNovice requires author.yearsOfExperience; author is owned by b but
        // yearsOfExperience is owned by a, so the requirement crosses an entity boundary.
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byNovice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_ByExpert_When_Circular_Require_Chains_Through_ByNovice()
    {
        // arrange
        var schema = CreateCircularCrossProviderSchema();

        // act
        // a.byExpert requires b.byNovice, which itself requires a.author.yearsOfExperience,
        // chaining a circular requirement across the entity boundary.
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byExpert
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_ByNovice_When_Both_Providers_Have_Author_Lookup()
    {
        // arrange
        // mirror of the cross-provider topology an Apollo _entities gateway produces, where
        // BOTH subgraphs expose an authorById lookup (one per @key entity). The re-rooted
        // author lookup could otherwise satisfy its own key (author.id) through b's authorById,
        // which forms a lookup cycle; the planner must resolve author.id through the parent
        // path (postById { author { id } }) so the plan stays acyclic.
        var schema = CreateCircularCrossProviderMirrorSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byNovice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_Author_When_Require_Crosses_List_Entity_Boundary()
    {
        // arrange
        var schema = CreateWithArgumentCrossProviderSchema();

        // act
        // d.author requires comments[authorId]; comments is owned by d but authorId is
        // owned by c, so the list requirement crosses an entity boundary.
        var plan = PlanOperation(
            schema,
            """
            {
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
    public void Plan_Should_Resolve_Author_When_Require_Uses_Input_Object_List_Form()
    {
        // arrange
        // the real Apollo composition form: the requirement is an object-shorthand list
        // map over a list field owned by one subgraph whose leaf is owned by another.
        var schema = CreateWithArgumentInputObjectCrossProviderSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
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
    public void Plan_Should_Resolve_ByNovice_When_Require_Is_Single_Provider()
    {
        // arrange
        // control: author and yearsOfExperience are both owned by a, so the requirement
        // stays within a single provider and plans today.
        var schema = CreateCircularSingleProviderSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byNovice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_NeedsFlag_When_Require_Is_Same_Entity_Direct_Field()
    {
        // arrange
        // control: needsFlag requires flag on the same entity (Post) from another
        // provider, a direct scalar with no nested boundary, so it plans today.
        var schema = CreateSameEntityDirectRequireSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                needsFlag
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateCircularCrossProviderSchema()
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
              authorById(id: ID! @is(field: "id")): Author @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              byExpert(byNovice: Boolean! @require(field: "byNovice")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
              name: String!
              yearsOfExperience: Int!
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
              author: Author!
              byNovice(
                yearsOfExperience: Int!
                  @require(field: "author.yearsOfExperience")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);
    }

    private static FusionSchemaDefinition CreateCircularCrossProviderMirrorSchema()
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
              authorById(id: ID! @is(field: "id")): Author @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              byExpert(byNovice: Boolean! @require(field: "byNovice")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
              name: String!
              yearsOfExperience: Int!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
              authorById(id: ID! @is(field: "id")): Author @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              author: Author!
              byNovice(
                yearsOfExperience: Int!
                  @require(field: "author.yearsOfExperience")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);
    }

    private static FusionSchemaDefinition CreateWithArgumentCrossProviderSchema()
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
              commentById(id: ID! @is(field: "id")): Comment @lookup @internal
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
            # name: d
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int): [Comment]
              author(
                commentAuthorIds: [ID]
                  @require(field: "comments[authorId]")): Author
            }

            type Comment @key(fields: "id") {
              id: ID!
            }

            type Author {
              id: ID!
              name: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateWithArgumentInputObjectCrossProviderSchema()
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
              commentById(id: ID! @is(field: "id")): Comment @lookup @internal
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
            # name: d
            schema {
              query: Query
            }

            type Query {
              postById(id: ID! @is(field: "id")): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int): [Comment]
              author(
                comments: [CommentInput]
                  @require(field: "comments(limit: 3)[{ authorId: authorId }]")): Author
            }

            type Comment @key(fields: "id") {
              id: ID!
            }

            type Author {
              id: ID!
              name: String
            }

            input CommentInput {
              authorId: ID
            }
            """);
    }

    private static FusionSchemaDefinition CreateCircularSingleProviderSchema()
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
              author: Author!
              byExpert(byNovice: Boolean! @require(field: "byNovice")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
              name: String!
              yearsOfExperience: Int!
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
              byNovice(
                yearsOfExperience: Int!
                  @require(field: "author.yearsOfExperience")): Boolean!
            }
            """);
    }

    private static FusionSchemaDefinition CreateSameEntityDirectRequireSchema()
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
              needsFlag(flag: Boolean! @require(field: "flag")): Boolean!
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
              flag: Boolean!
            }
            """);
    }
}
