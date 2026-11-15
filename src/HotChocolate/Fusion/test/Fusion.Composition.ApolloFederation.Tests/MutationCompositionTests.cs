using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class MutationCompositionTests
{
    [Fact]
    public void Compose_Should_AddLookupForMutationResult_When_AnotherSchemaResolvesItsFields()
    {
        // arrange
        const string ledger =
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              ping: String
            }

            type Mutation {
              bump(counter: ID!, by: Int!): Counter
            }

            type Counter @key(fields: "id") {
              id: ID!
            }
            """;

        const string reader =
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Counter @key(fields: "id") {
              id: ID!
              value: Int
            }
            """;

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("ledger", ledger), new SourceSchemaText("reader", reader)],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : string.Join("\n", result.Errors.Select(e => e.Message)));
        Assert.Empty(log);
        $"{result.Value.Types["Mutation"]}\n\n{result.Value.Types["Counter"]}".MatchInlineSnapshot(
            """
            type Mutation @fusion__type(schema: LEDGER) {
              bump(
                counter: ID! @fusion__inputField(schema: LEDGER)
                by: Int! @fusion__inputField(schema: LEDGER)
              ): Counter @fusion__field(schema: LEDGER)
            }

            type Counter
              @fusion__type(schema: LEDGER)
              @fusion__type(schema: READER)
              @fusion__lookup(
                schema: LEDGER
                key: "id"
                field: "fusion__lookup_counterById(id: ID!): Counter"
                map: ["id"]
                path: null
                internal: true
              )
              @fusion__lookup(
                schema: READER
                key: "id"
                field: "fusion__lookup_counterById(id: ID!): Counter"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: LEDGER) @fusion__field(schema: READER)
              value: Int @fusion__field(schema: READER)
            }
            """);
    }

    [Fact]
    public void Compose_Should_MergeMutationFields_When_TwoSchemasContributeRootMutations()
    {
        // arrange
        const string catalog =
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              items: [Item]
            }

            type Mutation {
              bump(counter: ID!, by: Int!): Int
            }

            type Item @key(fields: "id") {
              id: ID!
              name: String
            }
            """;

        const string details =
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Mutation {
              add(counter: ID!, by: Int!): Int
            }

            type Item @key(fields: "id") {
              id: ID!
              detail: Detail
            }

            type Detail {
              summary: String
              rating: Int
            }
            """;

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("catalog", catalog), new SourceSchemaText("details", details)],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : string.Join("\n", result.Errors.Select(e => e.Message)));
        Assert.Empty(log);
        result.Value.Types["Mutation"].ToString().MatchInlineSnapshot(
            """
            type Mutation @fusion__type(schema: CATALOG) @fusion__type(schema: DETAILS) {
              bump(
                counter: ID! @fusion__inputField(schema: CATALOG)
                by: Int! @fusion__inputField(schema: CATALOG)
              ): Int @fusion__field(schema: CATALOG)
              add(
                counter: ID! @fusion__inputField(schema: DETAILS)
                by: Int! @fusion__inputField(schema: DETAILS)
              ): Int @fusion__field(schema: DETAILS)
            }
            """);
    }
}
