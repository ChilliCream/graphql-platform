namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class EventStreamMessageInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EventStreamMessageInvalidFieldsRule();

    [Fact]
    public void Validate_EventStreamMessageObjectSelection_Succeeds()
    {
        AssertValid(
        [
            """
            type Subscription {
                bookChanged: Book
                    @eventStream(topics: ["book.changed"], message: "{ id author { name } }")
            }

            type Book {
                id: ID!
                author: Author!
            }

            type Author {
                name: String!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_EventStreamMessageAbstractSelection_Succeeds()
    {
        AssertValid(
        [
            """
            type Subscription {
                nodeChanged: Node
                    @eventStream(
                        topics: ["node.changed"]
                        message: "{ __typename ... on Book { id } ... on Author { id } }"
                    )
            }

            interface Node {
                id: ID!
            }

            type Book implements Node {
                id: ID!
            }

            type Author implements Node {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_EventStreamMessageUnknownField_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @eventStream(topics: ["book.changed"], message: "{ missing }")
                }

                type Book {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @eventStream directive on field 'Subscription.bookChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "EVENT_STREAM_MESSAGE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Subscription.bookChanged",
                    "member": "bookChanged",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'missing' does not exist on the type 'Book'."
                        ]
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_EventStreamMessageMissingObjectSubselection_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @eventStream(topics: ["book.changed"], message: "{ author }")
                }

                type Book {
                    author: Author!
                }

                type Author {
                    name: String!
                }
                """
            ],
            [
                """
                {
                    "message": "The @eventStream directive on field 'Subscription.bookChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "EVENT_STREAM_MESSAGE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Subscription.bookChanged",
                    "member": "bookChanged",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'author' returns a composite type and must have subselections."
                        ]
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_EventStreamMessageScalarSubselection_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @eventStream(topics: ["book.changed"], message: "{ id { value } }")
                }

                type Book {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @eventStream directive on field 'Subscription.bookChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "EVENT_STREAM_MESSAGE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Subscription.bookChanged",
                    "member": "bookChanged",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'id' does not return a composite type and cannot have subselections."
                        ]
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_EventStreamMessageInvalidTypeCondition_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    nodeChanged: Node
                        @eventStream(topics: ["node.changed"], message: "{ ... on Review { id } }")
                }

                interface Node {
                    id: ID!
                }

                type Book implements Node {
                    id: ID!
                }

                type Review {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @eventStream directive on field 'Subscription.nodeChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "EVENT_STREAM_MESSAGE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Subscription.nodeChanged",
                    "member": "nodeChanged",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The type 'Review' is not a possible type of type 'Node'."
                        ]
                    }
                }
                """
            ]);
    }
}
