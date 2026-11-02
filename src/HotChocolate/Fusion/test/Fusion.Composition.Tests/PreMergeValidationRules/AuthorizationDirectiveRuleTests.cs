namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class AuthorizationDirectiveRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new AuthorizationDirectiveRule();

    private const string Directives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    private const string PolicyDirective =
        """
        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Validate_Should_Succeed_When_AllSourcesMarkAuthenticated()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query @authenticated { field: String @authenticated }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query @authenticated { field: String @authenticated }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_NoSourceMarksAuthenticated()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query { field: String }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { field: String }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_WarnAuthenticatedMismatch_When_SourcesDisagreeOnField()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                # Schema A
                type Query { field: String @authenticated }

                {{Directives}}
                """,
                $$"""
                # Schema B
                type Query { field: String }

                {{Directives}}
                """
            ],
            [
                """
                {
                    "message": "The member 'Query.field' is marked with @authenticated in the source schemas 'A' but not in the source schemas 'B'. The composed member requires authentication.",
                    "code": "AUTHENTICATED_MISMATCH",
                    "severity": "Warning",
                    "coordinate": "Query.field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_WarnAuthenticatedMismatch_When_SourcesDisagreeOnType()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                # Schema A
                type Query { field: String }

                {{Directives}}
                """,
                $$"""
                # Schema B
                type Query @authenticated { field: String }

                {{Directives}}
                """,
                $$"""
                # Schema C
                type Query { field: String }

                {{Directives}}
                """
            ],
            [
                """
                {
                    "message": "The member 'Query' is marked with @authenticated in the source schemas 'B' but not in the source schemas 'A', 'C'. The composed member requires authentication.",
                    "code": "AUTHENTICATED_MISMATCH",
                    "severity": "Warning",
                    "coordinate": "Query",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_WarnGroupCountExceeded_When_ScopeProductExceedsThreshold()
    {
        // arrange
        var groupsA = string.Join(", ", Enumerable.Range(0, 9).Select(i => $"[\"a{i}\"]"));
        var groupsB = string.Join(", ", Enumerable.Range(0, 8).Select(i => $"[\"b{i}\"]"));

        // act & assert
        AssertInvalid(
            [
                $$"""
                # Schema A
                type Query { field: String @requiresScopes(scopes: [{{groupsA}}]) }

                {{Directives}}
                """,
                $$"""
                # Schema B
                type Query { field: String @requiresScopes(scopes: [{{groupsB}}]) }

                {{Directives}}
                """
            ],
            [
                """
                {
                    "message": "The merged scopes requirement of 'Query.field' has 72 alternative groups, which exceeds the threshold of 64.",
                    "code": "AUTHORIZATION_GROUP_COUNT_EXCEEDED",
                    "severity": "Warning",
                    "coordinate": "Query.field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_WarnGroupCountExceeded_When_PolicyProductExceedsThreshold()
    {
        // arrange
        var groupsA = string.Join(", ", Enumerable.Range(0, 9).Select(i => $"[\"a{i}\"]"));
        var groupsB = string.Join(", ", Enumerable.Range(0, 8).Select(i => $"[\"b{i}\"]"));

        // act & assert
        AssertInvalid(
            [
                $$"""
                # Schema A
                type Query { field: String @policy(policies: [{{groupsA}}]) }

                {{PolicyDirective}}
                """,
                $$"""
                # Schema B
                type Query { field: String @policy(policies: [{{groupsB}}]) }

                {{PolicyDirective}}
                """
            ],
            [
                """
                {
                    "message": "The merged policies requirement of 'Query.field' has 72 alternative groups, which exceeds the threshold of 64.",
                    "code": "AUTHORIZATION_GROUP_COUNT_EXCEEDED",
                    "severity": "Warning",
                    "coordinate": "Query.field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
