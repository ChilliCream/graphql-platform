namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class EventStreamTopicsEmptyRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EventStreamTopicsEmptyRule();

    [Fact]
    public void Validate_TopicsOmitted_Succeeds()
    {
        AssertValid(
        [
            """
            type Subscription {
                bookChanged: Book
                    @eventStream(message: "{ id }")
            }

            type Book {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_TopicsProvided_Succeeds()
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
    public void Validate_TopicsEmptyList_Fails()
    {
        AssertInvalid(
            [
                """
                type Subscription {
                    bookChanged: Book
                        @eventStream(topics: [], message: "{ id }")
                }

                type Book {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @eventStream directive on field 'Subscription.bookChanged' in schema 'A' must not declare an empty list of topics. Omit the 'topics' argument to derive topics automatically.",
                    "code": "EVENT_STREAM_TOPICS_EMPTY",
                    "severity": "Error",
                    "coordinate": "Subscription.bookChanged",
                    "member": "bookChanged",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
