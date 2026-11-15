using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilitySourceSchemaNodeTests
{
    [Fact]
    public void Validate_Should_Succeed_When_NodeLookupOfKeyOwnerResolvesEveryFieldOfType()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    node(id: ID!): Node @lookup @shareable
                }

                interface Node {
                    id: ID!
                }

                type Account implements Node @key(fields: "id") {
                    id: ID!
                    username: String!
                }

                type Chat implements Node @key(fields: "id") {
                    id: ID! @external
                    account: Account!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    node(id: ID!): Node @lookup @shareable
                }

                interface Node {
                    id: ID!
                }

                type Account implements Node @key(fields: "id") {
                    id: ID! @external
                    chats: [Chat!]!
                }

                type Chat implements Node @key(fields: "id") {
                    id: ID!
                    text: String!
                }
                """)
        ];

        // act
        var (result, log) = Compose(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_NodeTypeKeyIsOnlyOwnedBySchemaWithoutNodeLookup()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    node(id: ID!): Node @lookup
                }

                interface Node {
                    id: ID!
                }

                type Chat implements Node @key(fields: "id") {
                    id: ID! @external
                    title: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    chatById(id: ID!): Chat @lookup @internal
                }

                type Chat @key(fields: "id") {
                    id: ID!
                    text: String!
                }
                """)
        ];

        // act
        var (result, log) = Compose(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Chat.id' on path '*:Query.node<Node> -> A:Query.node<Node>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Chat.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'chatById' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Chat.id' on path 'A:Query.node<Node>'.
                      No other schemas contain the field 'Chat.id'.
            """,
            """
            Unable to access the field 'Chat.text' on path '*:Query.node<Node> -> A:Query.node<Node>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Chat.text<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'chatById' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Chat.id' on path 'A:Query.node<Node>'.
                      No other schemas contain the field 'Chat.id'.
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_NodeInterfaceHasUniformlyOwnedFieldAndNoNodeLookupResolvesEveryType()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    node(id: ID!): Node @lookup @shareable
                }

                interface Node {
                    id: ID!
                    label: String
                }

                type Account implements Node @key(fields: "id") {
                    id: ID!
                    label: String @shareable
                    username: String!
                }

                type Chat implements Node @key(fields: "id") {
                    id: ID! @external
                    label: String @shareable
                    account: Account!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    node(id: ID!): Node @lookup @shareable
                }

                interface Node {
                    id: ID!
                    label: String
                }

                type Account implements Node @key(fields: "id") {
                    id: ID! @external
                    label: String @shareable
                    chats: [Chat!]!
                }

                type Chat implements Node @key(fields: "id") {
                    id: ID!
                    label: String @shareable
                    text: String!
                }
                """)
        ];

        // act
        var (result, log) = Compose(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Chat.id' on path '*:Query.node<Node> -> A:Query.node<Node>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Chat.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'node' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Chat.id' on path 'A:Query.node<Node>'.
                      No other schemas contain the field 'Chat.id'.
            """,
            """
            Unable to access the field 'Chat.text' on path '*:Query.node<Node> -> A:Query.node<Node>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Chat.text<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'node' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Chat.id' on path 'A:Query.node<Node>'.
                      No other schemas contain the field 'Chat.id'.
            """,
            """
            Unable to access the field 'Account.id' on path '*:Query.node<Node> -> B:Query.node<Node>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Account.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'node' in schema 'A'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Account.id' on path 'B:Query.node<Node>'.
                      No other schemas contain the field 'Account.id'.
            """,
            """
            Unable to access the field 'Account.username' on path '*:Query.node<Node> -> B:Query.node<Node>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Account.username<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'node' in schema 'A'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Account.id' on path 'B:Query.node<Node>'.
                      No other schemas contain the field 'Account.id'.
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_ExternalKeyIsOnlyKeySourceForLookup()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    orders: [Order!]!
                }

                type Order @key(fields: "id") {
                    id: ID! @external
                    note: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    orderById(id: ID!): Order @lookup @internal
                }

                type Order @key(fields: "id") {
                    id: ID!
                    status: String!
                }
                """)
        ];

        // act
        var (result, log) = Compose(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Order.id' on path 'A:Query.orders<Order>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Order.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'orderById' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Order.id' on path 'A:Query.orders<Order>'.
                      No other schemas contain the field 'Order.id'.
            """,
            """
            Unable to access the field 'Order.status' on path 'A:Query.orders<Order>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Order.status<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'orderById' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Order.id' on path 'A:Query.orders<Order>'.
                      No other schemas contain the field 'Order.id'.
            """
        ]);
    }

    private static (CompositionResult<MutableSchemaDefinition> Result, CompositionLog Log) Compose(
        SourceSchemaText[] sourceSchemas)
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            sourceSchemas,
            new SchemaComposerOptions
            {
                Merger =
                {
                    AddFusionDefinitions = false,
                    EnableGlobalObjectIdentification = true,
                    NodeResolution = NodeResolution.SourceSchema
                },
                Satisfiability = { IncludeSatisfiabilityPaths = true }
            },
            log);

        return (composer.Compose(), log);
    }
}
