using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerEventStreamTests : SourceSchemaMergerTestBase
{
    [Fact]
    public void Merge_EventStream_Should_Collapse_When_ShareableDeclarationsAreIdentical()
    {
        AssertMatches(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @shareable
                        @eventStream(
                            topics: ["book.changed", "book.updated"]
                            broker: "events"
                            message: "{ id }"
                        )
                }

                type Book {
                    id: ID!
                }
                """,
                """
                type Subscription {
                    bookChanged: Book
                        @shareable
                        @eventStream(
                            topics: ["book.updated", "book.changed"]
                            broker: "events"
                            message: "{ id }"
                        )
                }

                type Book {
                    id: ID!
                }
                """
            ],
            """
            schema {
              subscription: Subscription
            }

            type Subscription @fusion__type(schema: A) @fusion__type(schema: B) {
              bookChanged: Book
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__eventStream(
                  schema: A
                  topics: ["book.changed", "book.updated"]
                  broker: "events"
                  message: "{ id }"
                )
            }

            type Book @fusion__type(schema: A) @fusion__type(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_EventStream_Should_InferTopics_When_TopicsOmittedWithMultipleArguments()
    {
        AssertMatches(
            [
                """
                type Subscription {
                    onUserCreated(after: String @eventCursor, a: String, b: String): User
                        @eventStream(message: "{ id }")
                }

                type User {
                    id: ID!
                }
                """
            ],
            """
            schema {
              subscription: Subscription
            }

            type Subscription @fusion__type(schema: A) {
              onUserCreated(
                a: String @fusion__inputField(schema: A)
                after: String @fusion__inputField(schema: A)
                b: String @fusion__inputField(schema: A)
              ): User
                @fusion__field(schema: A)
                @fusion__eventStream(
                  schema: A
                  topics: ["onUserCreated-{$args.a}-{$args.b}"]
                  message: "{ id }"
                  cursorArgument: "after"
                )
            }

            type User @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_EventStream_Should_InferFieldNameOnly_When_TopicsOmittedWithCursorArgumentOnly()
    {
        AssertMatches(
            [
                """
                type Subscription {
                    onUserCreated(after: String @eventCursor): User
                        @eventStream(message: "{ id }")
                }

                type User {
                    id: ID!
                }
                """
            ],
            """
            schema {
              subscription: Subscription
            }

            type Subscription @fusion__type(schema: A) {
              onUserCreated(after: String @fusion__inputField(schema: A)): User
                @fusion__field(schema: A)
                @fusion__eventStream(
                  schema: A
                  topics: ["onUserCreated"]
                  message: "{ id }"
                  cursorArgument: "after"
                )
            }

            type User @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_EventStream_Should_InferFieldNameAndArgument_When_TopicsOmittedWithSingleArgument()
    {
        AssertMatches(
            [
                """
                type Subscription {
                    onUserCreated(id: String!): User
                        @eventStream(message: "{ id }")
                }

                type User {
                    id: ID!
                }
                """
            ],
            """
            schema {
              subscription: Subscription
            }

            type Subscription @fusion__type(schema: A) {
              onUserCreated(id: String! @fusion__inputField(schema: A)): User
                @fusion__field(schema: A)
                @fusion__eventStream(
                  schema: A
                  topics: ["onUserCreated-{$args.id}"]
                  message: "{ id }"
                )
            }

            type User @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_EventStream_Should_PreserveTopics_When_TopicsProvided()
    {
        AssertMatches(
            [
                """
                type Subscription {
                    onUserCreated(id: String): User
                        @eventStream(topics: ["custom.topic"], message: "{ id }")
                }

                type User {
                    id: ID!
                }
                """
            ],
            """
            schema {
              subscription: Subscription
            }

            type Subscription @fusion__type(schema: A) {
              onUserCreated(id: String @fusion__inputField(schema: A)): User
                @fusion__field(schema: A)
                @fusion__eventStream(schema: A, topics: ["custom.topic"], message: "{ id }")
            }

            type User @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_EventStream_Should_Collapse_When_ReturnTypesDifferOnlyByNullability()
    {
        AssertMatches(
            [
                """
                type Subscription {
                    bookChanged: Book!
                        @shareable
                        @eventStream(topics: ["book.changed"], message: "{ id }")
                }

                type Book {
                    id: ID!
                }
                """,
                """
                type Subscription {
                    bookChanged: Book
                        @shareable
                        @eventStream(topics: ["book.changed"], message: "{ id }")
                }

                type Book {
                    id: ID!
                }
                """
            ],
            """
            schema {
              subscription: Subscription
            }

            type Subscription @fusion__type(schema: A) @fusion__type(schema: B) {
              bookChanged: Book
                @fusion__field(schema: A, sourceType: "Book!")
                @fusion__field(schema: B)
                @fusion__eventStream(schema: A, topics: ["book.changed"], message: "{ id }")
            }

            type Book @fusion__type(schema: A) @fusion__type(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Compose_EventStream_Should_Fail_When_ShareableDeclarationsDiffer()
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Subscription {
                        bookChanged: Book
                            @shareable
                            @eventStream(topics: ["book.changed"], message: "{ id }")
                    }

                    type Book {
                        id: ID!
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Subscription {
                        bookChanged: Book
                            @shareable
                            @eventStream(topics: ["book.updated"], message: "{ id }")
                    }

                    type Book {
                        id: ID!
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        var result = composer.Compose();

        Assert.True(result.IsFailure);
        var entry = Assert.Single(log, t => t.Code == LogEntryCodes.MultipleEventStreamSources);
        Assert.Equal(LogSeverity.Error, entry.Severity);
    }

    [Fact]
    public void Compose_EventStream_Should_Fail_When_ReturnTypesDifferBeyondNullability()
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Subscription {
                        changed: Book
                            @shareable
                            @eventStream(topics: ["node.changed"], message: "{ id }")
                    }

                    interface Node {
                        id: ID!
                    }

                    type Book implements Node {
                        id: ID! @shareable
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Subscription {
                        changed: Node
                            @shareable
                            @eventStream(topics: ["node.changed"], message: "{ id }")
                    }

                    interface Node {
                        id: ID!
                    }

                    type Book implements Node {
                        id: ID! @shareable
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        var result = composer.Compose();

        Assert.True(result.IsFailure);
        log.Select(t => t.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The output field 'Subscription.changed' has a different type shape in schema 'A' than it does in schema 'B'.",
                "code": "OUTPUT_FIELD_TYPES_NOT_MERGEABLE",
                "severity": "Error",
                "coordinate": "Subscription.changed",
                "member": "changed",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_EventStream_Should_Fail_With_InvalidFieldSharing_When_NotShareable()
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Subscription {
                        bookChanged: Book
                            @eventStream(topics: ["book.changed"], message: "{ id }")
                    }

                    type Book {
                        id: ID! @shareable
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Subscription {
                        bookChanged: Book
                            @eventStream(topics: ["book.changed"], message: "{ id }")
                    }

                    type Book {
                        id: ID! @shareable
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        var result = composer.Compose();

        Assert.True(result.IsFailure);
        log.Select(t => t.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Subscription.bookChanged' in schema 'A' must be shareable.",
                "code": "INVALID_FIELD_SHARING",
                "severity": "Error",
                "coordinate": "Subscription.bookChanged",
                "member": "bookChanged",
                "schema": "A",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The field 'Subscription.bookChanged' in schema 'B' must be shareable.",
                "code": "INVALID_FIELD_SHARING",
                "severity": "Error",
                "coordinate": "Subscription.bookChanged",
                "member": "bookChanged",
                "schema": "B",
                "extensions": {}
            }
            """
        ]);
    }
}
