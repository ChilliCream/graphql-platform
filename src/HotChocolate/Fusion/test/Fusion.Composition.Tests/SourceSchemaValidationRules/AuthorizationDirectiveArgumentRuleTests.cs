using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class AuthorizationDirectiveArgumentRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new AuthorizationDirectiveArgumentRule();

    private const string Directives =
        """
        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Validate_Should_Succeed_When_ArgumentsUseCanonicalAndCoercibleShapes()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            type Query {
                a: Int @requiresScopes(scopes: [["read", "write"], ["admin"]])
                b: Int @requiresScopes(scopes: "read")
                c: Int @requiresScopes(scopes: ["read", ["write"]])
                d: Int @policy(policies: "p")
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_ScopesIsNotAStringOrList()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                type Query {
                    field: Int @requiresScopes(scopes: 1)
                }

                {{Directives}}
                """
            ],
            [
                """
                {
                    "message": "The 'scopes' argument of the @requiresScopes directive on 'Query.field' in schema 'A' must be a string or a list of strings and string lists, but has the value 1.",
                    "code": "INVALID_AUTHORIZATION_DIRECTIVE_ARGUMENT",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_ArgumentIsMissingOrNullOnTypes()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                type Query @requiresScopes {
                    field: Foo
                }

                scalar Foo @policy(policies: null)

                enum Bar @policy(policies: [[1]]) {
                    ONE
                }

                {{Directives}}
                """
            ],
            [
                """
                {
                    "message": "The 'scopes' argument of the @requiresScopes directive on 'Query' in schema 'A' must be a string or a list of strings and string lists, but has the value undefined.",
                    "code": "INVALID_AUTHORIZATION_DIRECTIVE_ARGUMENT",
                    "severity": "Error",
                    "coordinate": "Query",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The 'policies' argument of the @policy directive on 'Foo' in schema 'A' must be a string or a list of strings and string lists, but has the value null.",
                    "code": "INVALID_AUTHORIZATION_DIRECTIVE_ARGUMENT",
                    "severity": "Error",
                    "coordinate": "Foo",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The 'policies' argument of the @policy directive on 'Bar' in schema 'A' must be a string or a list of strings and string lists, but has the value [[1]].",
                    "code": "INVALID_AUTHORIZATION_DIRECTIVE_ARGUMENT",
                    "severity": "Error",
                    "coordinate": "Bar",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_ScopesArgumentIsMissing()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    $$"""
                    type Query {
                        field: Int @requiresScopes
                    }

                    {{Directives}}
                    """)
            ],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The 'scopes' argument of the @requiresScopes directive on 'Query.field' in schema 'A' must be a string or a list of strings and string lists, but has the value undefined.",
                "code": "INVALID_AUTHORIZATION_DIRECTIVE_ARGUMENT",
                "severity": "Error",
                "coordinate": "Query.field",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }
}
