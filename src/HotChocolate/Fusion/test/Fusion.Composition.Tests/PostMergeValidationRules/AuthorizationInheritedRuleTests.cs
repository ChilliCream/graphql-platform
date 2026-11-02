namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class AuthorizationInheritedRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new AuthorizationInheritedRule();

    private const string Directives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Validate_Should_Succeed_When_NoMemberIsProtectedByInheritance()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query { search: SearchResult }

            interface SearchResult { title: String @authenticated }

            type Article implements SearchResult { title: String @authenticated }

            type Video implements SearchResult { title: String @authenticated }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_WarnForUnannotatedFields_When_ImplementationFieldIsProtected()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                # Schema A
                type Query { search: SearchResult }

                interface SearchResult { title: String }

                type Article implements SearchResult { title: String @authenticated }

                {{Directives}}
                """,
                $$"""
                # Schema B
                type Query { video: Video }

                type Video implements SearchResult { title: String }

                interface SearchResult { title: String }

                {{Directives}}
                """
            ],
            [
                """
                {
                  "message": "The member 'SearchResult.title' requires authorization through interface inheritance, but no source schema annotated it. The requirement comes from Article.title -> SearchResult.title in the source schemas 'A'.",
                  "code": "AUTHORIZATION_INHERITED",
                  "severity": "Warning",
                  "coordinate": "SearchResult.title",
                  "extensions": {}
                }
                """,
                """
                {
                  "message": "The member 'Video.title' requires authorization through interface inheritance, but no source schema annotated it. The requirement comes from Article.title -> SearchResult.title -> Video.title in the source schemas 'A'.",
                  "code": "AUTHORIZATION_INHERITED",
                  "severity": "Warning",
                  "coordinate": "Video.title",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_WarnForFieldsOnly_When_ImplementationTypeIsProtected()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                # Schema A
                type Query { search: SearchResult }

                interface SearchResult { title: String }

                type Article implements SearchResult @requiresScopes(scopes: [["read"]]) {
                    title: String
                }

                type Video implements SearchResult { title: String }

                {{Directives}}
                """
            ],
            [
                """
                {
                  "message": "The member 'SearchResult.title' requires authorization through interface inheritance, but no source schema annotated it. The requirement comes from Article.title -> SearchResult.title in the source schemas 'A'.",
                  "code": "AUTHORIZATION_INHERITED",
                  "severity": "Warning",
                  "coordinate": "SearchResult.title",
                  "extensions": {}
                }
                """,
                """
                {
                  "message": "The member 'Video.title' requires authorization through interface inheritance, but no source schema annotated it. The requirement comes from Article.title -> SearchResult.title -> Video.title in the source schemas 'A'.",
                  "code": "AUTHORIZATION_INHERITED",
                  "severity": "Warning",
                  "coordinate": "Video.title",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_WarnForUnannotatedType_When_InterfaceTypeIsProtected()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                # Schema A
                type Query { video: Video }

                interface SearchResult @authenticated { title: String }

                type Video implements SearchResult { title: String }

                {{Directives}}
                """
            ],
            [
                """
                {
                  "message": "The member 'Video' requires authorization through interface inheritance, but no source schema annotated it. The requirement comes from SearchResult -> Video in the source schemas 'A'.",
                  "code": "AUTHORIZATION_INHERITED",
                  "severity": "Warning",
                  "coordinate": "Video",
                  "extensions": {}
                }
                """
            ]);
    }
}
