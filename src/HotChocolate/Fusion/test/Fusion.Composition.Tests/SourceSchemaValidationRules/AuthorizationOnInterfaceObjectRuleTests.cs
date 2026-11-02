namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class AuthorizationOnInterfaceObjectRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new AuthorizationOnInterfaceObjectRule();

    private const string Directives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Validate_Should_Succeed_When_OnlyTheFieldsOfTheInterfaceObjectAreAnnotated()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            type Query { media: Media }

            type Media @interfaceObject @key(fields: "id") {
                id: ID!
                reviews: [String!]! @authenticated @requiresScopes(scopes: [["read"]])
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_AnOrdinaryObjectTypeIsAnnotated()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            type Query { media: Media }

            type Media @authenticated @key(fields: "id") {
                id: ID!
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheInterfaceObjectTypeIsAnnotated()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                $$"""
                type Query { media: Media }

                type Media @interfaceObject @key(fields: "id")
                    @authenticated @requiresScopes(scopes: [["read"]]) @policy(policies: [["p"]]) {
                    id: ID!
                }

                {{Directives}}
                """
            ],
            [
                """
                {
                  "message": "The @interfaceObject type 'Media' in schema 'A' cannot be annotated with @authenticated. Annotate its fields instead.",
                  "code": "AUTHORIZATION_ON_INTERFACE_OBJECT",
                  "severity": "Error",
                  "coordinate": "Media",
                  "member": "Media",
                  "schema": "A",
                  "extensions": {}
                }
                """,
                """
                {
                  "message": "The @interfaceObject type 'Media' in schema 'A' cannot be annotated with @requiresScopes. Annotate its fields instead.",
                  "code": "AUTHORIZATION_ON_INTERFACE_OBJECT",
                  "severity": "Error",
                  "coordinate": "Media",
                  "member": "Media",
                  "schema": "A",
                  "extensions": {}
                }
                """,
                """
                {
                  "message": "The @interfaceObject type 'Media' in schema 'A' cannot be annotated with @policy. Annotate its fields instead.",
                  "code": "AUTHORIZATION_ON_INTERFACE_OBJECT",
                  "severity": "Error",
                  "coordinate": "Media",
                  "member": "Media",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }
}
