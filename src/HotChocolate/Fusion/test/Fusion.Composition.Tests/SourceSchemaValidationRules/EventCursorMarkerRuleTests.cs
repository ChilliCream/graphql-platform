namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class EventCursorMarkerRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EventCursorMarkerRule();

    [Fact]
    public void Validate_CursorMarkersValid_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                version: String
            }

            type Subscription {
                onUserChanged(after: String! @eventCursor): UserChangedEvent
                    @eventStream(message: "{ id changeType }")
            }

            type UserChangedEvent {
                id: ID!
                changeType: String!
                cursor: String! @eventCursor
            }
            """
        ]);
    }

    [Fact]
    public void Validate_CursorListTypes_Fail()
    {
        AssertInvalid(
        [
            """
            type Query {
                version: String
            }

            type Subscription {
                onUserChanged(after: [String!] @eventCursor): UserChangedEvent
                    @eventStream(message: "{ id changeType }")
            }

            type UserChangedEvent {
                id: ID!
                changeType: String!
                cursor: [String] @eventCursor
            }
            """
        ],
        [
            """
            {
                "message": "The @eventCursor argument 'Subscription.onUserChanged(after:)' in schema 'A' must be of type String.",
                "code": "CURSOR_ARGUMENT_NOT_STRING",
                "severity": "Error",
                "coordinate": "Subscription.onUserChanged(after:)",
                "member": "after",
                "schema": "A",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The @eventCursor field 'UserChangedEvent.cursor' in schema 'A' must be of type String.",
                "code": "CURSOR_FIELD_NOT_STRING",
                "severity": "Error",
                "coordinate": "UserChangedEvent.cursor",
                "member": "cursor",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_CursorArgumentWithCursorField_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                version: String
            }

            type Subscription {
                onUserChanged(after: String @eventCursor): UserChangedEvent
                    @eventStream(message: "{ id changeType }")
            }

            type UserChangedEvent {
                id: ID!
                changeType: String!
                cursor: String @eventCursor
            }
            """
        ]);
    }

    [Fact]
    public void Validate_CursorFieldWithoutArgument_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                version: String
            }

            type Subscription {
                onUserChanged: UserChangedEvent
                    @eventStream(message: "{ id changeType }")
            }

            type UserChangedEvent {
                id: ID!
                changeType: String!
                cursor: String @eventCursor
            }
            """
        ]);
    }

    [Fact]
    public void Validate_CursorArgumentWithoutCursorField_Fails()
    {
        AssertInvalid(
        [
            """
            type Query {
                version: String
            }

            type Subscription {
                onUserChanged(after: String @eventCursor): UserChangedEvent
                    @eventStream(message: "{ id changeType }")
            }

            type UserChangedEvent {
                id: ID!
                changeType: String!
            }
            """
        ],
        [
            """
            {
                "message": "The @eventCursor argument on field 'Subscription.onUserChanged' in schema 'A' requires an @eventCursor field on the event payload type.",
                "code": "CURSOR_ARGUMENT_REQUIRES_CURSOR_FIELD",
                "severity": "Error",
                "coordinate": "Subscription.onUserChanged",
                "member": "onUserChanged",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_MultipleCursorFields_Fails()
    {
        AssertInvalid(
        [
            """
            type Query {
                version: String
            }

            type Subscription {
                onUserChanged: UserChangedEvent
                    @eventStream(message: "{ id changeType }")
            }

            type UserChangedEvent {
                id: ID!
                changeType: String!
                cursor: String @eventCursor
                position: String @eventCursor
            }
            """
        ],
        [
            """
            {
                "message": "The @eventStream field 'Subscription.onUserChanged' in schema 'A' must not declare more than one @eventCursor field on its return type.",
                "code": "MULTIPLE_CURSOR_FIELDS",
                "severity": "Error",
                "coordinate": "Subscription.onUserChanged",
                "member": "onUserChanged",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_MultipleCursorArguments_Fails()
    {
        AssertInvalid(
        [
            """
            type Query {
                version: String
            }

            type Subscription {
                onUserChanged(after: String @eventCursor, cursor: String @eventCursor): UserChangedEvent
                    @eventStream(message: "{ id changeType }")
            }

            type UserChangedEvent {
                id: ID!
                changeType: String!
            }
            """
        ],
        [
            """
            {
                "message": "The @eventStream field 'Subscription.onUserChanged' in schema 'A' must not declare more than one @eventCursor argument.",
                "code": "MULTIPLE_CURSOR_ARGUMENTS",
                "severity": "Error",
                "coordinate": "Subscription.onUserChanged",
                "member": "onUserChanged",
                "schema": "A",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The @eventCursor argument on field 'Subscription.onUserChanged' in schema 'A' requires an @eventCursor field on the event payload type.",
                "code": "CURSOR_ARGUMENT_REQUIRES_CURSOR_FIELD",
                "severity": "Error",
                "coordinate": "Subscription.onUserChanged",
                "member": "onUserChanged",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_CursorMarkerOnNonSubscriptionField_Fails()
    {
        AssertInvalid(
        [
            """
            type Query {
                search(after: String @eventCursor): SearchResult
                cursor: String @eventCursor
            }

            type SearchResult {
                id: ID!
            }
            """
        ],
        [
            """
            {
                "message": "The @eventCursor marker 'Query.search(after:)' in schema 'A' must be reachable from an @eventStream subscription field.",
                "code": "CURSOR_MARKER_ON_NON_SUBSCRIPTION_FIELD",
                "severity": "Error",
                "coordinate": "Query.search(after:)",
                "member": "after",
                "schema": "A",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The @eventCursor marker 'Query.cursor' in schema 'A' must be reachable from an @eventStream subscription field.",
                "code": "CURSOR_MARKER_ON_NON_SUBSCRIPTION_FIELD",
                "severity": "Error",
                "coordinate": "Query.cursor",
                "member": "cursor",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }
}
