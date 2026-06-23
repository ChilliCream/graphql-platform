namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class SubscribeMessageInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new SubscribeMessageInvalidFieldsRule();

    [Fact]
    public void Validate_SubscribeMessageObjectSelection_Succeeds()
    {
        AssertValid(
        [
            """
            type Subscription {
                bookChanged: Book
                    @subscribe(topics: ["book.changed"], message: "{ id author { name } }")
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
    public void Validate_SubscribeMessageAbstractSelection_Succeeds()
    {
        AssertValid(
        [
            """
            type Subscription {
                nodeChanged: Node
                    @subscribe(
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
    public void Validate_SubscribeMessageUnknownField_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @subscribe(topics: ["book.changed"], message: "{ missing }")
                }

                type Book {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @subscribe directive on field 'Subscription.bookChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "SUBSCRIBE_MESSAGE_INVALID_FIELDS",
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
    public void Validate_SubscribeMessageMissingObjectSubselection_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @subscribe(topics: ["book.changed"], message: "{ author }")
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
                    "message": "The @subscribe directive on field 'Subscription.bookChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "SUBSCRIBE_MESSAGE_INVALID_FIELDS",
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
    public void Validate_SubscribeMessageScalarSubselection_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @subscribe(topics: ["book.changed"], message: "{ id { value } }")
                }

                type Book {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @subscribe directive on field 'Subscription.bookChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "SUBSCRIBE_MESSAGE_INVALID_FIELDS",
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
    public void Validate_SubscribeMessageInvalidTypeCondition_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    nodeChanged: Node
                        @subscribe(topics: ["node.changed"], message: "{ ... on Review { id } }")
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
                    "message": "The @subscribe directive on field 'Subscription.nodeChanged' in schema 'A' specifies an invalid message selection.",
                    "code": "SUBSCRIBE_MESSAGE_INVALID_FIELDS",
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
