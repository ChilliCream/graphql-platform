namespace HotChocolate.Fusion.Planning;

public sealed class ApolloSelfRequirementPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_PassExternalKeyToRequiringSubgraph_When_RequiresSelectsExternalKeyField()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: requiring
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Account @key(fields: "id") {
              id: ID! @external
              displayName: String! @requires(fields: "id")
            }
            """,
            """
            # name: other
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Query {
              accounts: [Account]
            }

            type Account @key(fields: "id") {
              id: ID!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              accounts {
                id
                displayName
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_FetchRequiredParentFromOtherSubgraph_When_RequiresSelectsExternalParentField()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Query {
              orders: [Order]
            }

            type Order @key(fields: "id") {
              id: ID!
              customer: Customer @external
              price: Int @requires(fields: "customer { discount }")
            }

            type Customer @key(fields: "id") {
              id: ID!
              discount: Int @external
            }
            """,
            """
            # name: b
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Order @key(fields: "id") {
              id: ID!
              customer: Customer
            }

            type Customer @key(fields: "id") {
              id: ID!
              discount: Int
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              orders {
                id
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ChainRequirements_When_CircularRequirementReadsParentFromThirdSubgraph()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Query {
              feed: [Post]
            }

            type Post @key(fields: "id") {
              id: ID!
              byNovice: Boolean! @external
              byExpert: Boolean! @requires(fields: "byNovice")
            }

            type Author @key(fields: "id") {
              id: ID!
              name: String!
              yearsOfExperience: Int!
            }
            """,
            """
            # name: b
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Post @key(fields: "id") {
              id: ID!
              author: Author!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Post @key(fields: "id") {
              id: ID!
              author: Author! @external
              byNovice: Boolean! @requires(fields: "author { yearsOfExperience }")
            }

            type Author @key(fields: "id") {
              id: ID!
              yearsOfExperience: Int! @external
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                id
                byNovice
                byExpert
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_FetchRequiredFieldWithArgumentFromOtherSubgraph_When_RequiringSubgraphDeclaresItExternal()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: c
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Query {
              feed: [Post]
            }

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int!): [Comment]
            }

            type Comment @key(fields: "id") {
              id: ID!
              authorId: ID
              body: String
            }
            """,
            """
            # name: d
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Post @key(fields: "id") {
              id: ID!
              comments(limit: Int!): [Comment] @external
              author: Author @requires(fields: "comments(limit: 3) { authorId }")
            }

            type Comment @key(fields: "id") {
              id: ID!
              authorId: ID @external
            }

            type Author {
              id: ID!
              name: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                id
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
}
