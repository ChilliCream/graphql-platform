namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EventStreamMessageAbstractTypeRequiresTypeNameRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EventStreamMessageAbstractTypeRequiresTypeNameRule();

    [Fact]
    public void Validate_EventStreamMessageConcreteSelection_Succeeds()
    {
        AssertValid(
        [
            """
            type Subscription {
                bookChanged: Book
                    @eventStream(topics: ["book.changed"], message: "{ id }")
            }

            type Book {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_EventStreamMessageAbstractSelectionWithTypeName_Succeeds()
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
    public void Validate_EventStreamMessageAbstractSelectionWithoutTypeName_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    nodeChanged: Node
                        @eventStream(
                            topics: ["node.changed"]
                            message: "{ ... on Book { id } ... on Author { id } }"
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
            ],
            [
                """
                {
                    "message": "The @eventStream directive on field 'Subscription.nodeChanged' in schema 'A' selects an abstract type but its message does not include '__typename' at that level, which is required to resolve the concrete type at runtime.",
                    "code": "EVENT_STREAM_MESSAGE_ABSTRACT_TYPE_REQUIRES_TYPENAME",
                    "severity": "Error",
                    "coordinate": "Subscription.nodeChanged",
                    "member": "nodeChanged",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
