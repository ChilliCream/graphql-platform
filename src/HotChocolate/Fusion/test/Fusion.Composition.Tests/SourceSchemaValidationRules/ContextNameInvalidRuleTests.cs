namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ContextNameInvalidRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ContextNameInvalidRule();

    [Fact]
    public void Validate_Should_Succeed_When_TheContextNamesContainOnlyLettersAndDigits()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Query {
                user: User
                media: Media
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
            }

            interface Media @key(fields: "id") @context(name: "Media2") {
                id: ID!
            }

            union Result @context(name: "a") = User
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextNameContainsAnUnderscore()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Query {
                    user: User
                }

                type User @key(fields: "id") @context(name: "user_context") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                  "message": "The @context directive on type 'User' in schema 'A' has the invalid name 'user_context'. A context name must start with a letter and contain only letters and digits.",
                  "code": "CONTEXT_NAME_INVALID",
                  "severity": "Error",
                  "coordinate": "User",
                  "member": "context",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextNameStartsWithADigit()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Query {
                    user: User
                }

                type User @key(fields: "id") @context(name: "1user") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                  "message": "The @context directive on type 'User' in schema 'A' has the invalid name '1user'. A context name must start with a letter and contain only letters and digits.",
                  "code": "CONTEXT_NAME_INVALID",
                  "severity": "Error",
                  "coordinate": "User",
                  "member": "context",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextNameIsEmpty()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Query {
                    user: User
                }

                type User @key(fields: "id") @context(name: "") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                  "message": "The @context directive on type 'User' in schema 'A' has the invalid name ''. A context name must start with a letter and contain only letters and digits.",
                  "code": "CONTEXT_NAME_INVALID",
                  "severity": "Error",
                  "coordinate": "User",
                  "member": "context",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_InterfaceAndUnionContextNamesAreInvalid()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Query {
                    media: Media
                    result: Result
                }

                interface Media @key(fields: "id") @context(name: "media-info") {
                    id: ID!
                }

                type User {
                    id: ID!
                }

                union Result @context(name: "_result") = User
                """
            ],
            [
                """
                {
                  "message": "The @context directive on type 'Media' in schema 'A' has the invalid name 'media-info'. A context name must start with a letter and contain only letters and digits.",
                  "code": "CONTEXT_NAME_INVALID",
                  "severity": "Error",
                  "coordinate": "Media",
                  "member": "context",
                  "schema": "A",
                  "extensions": {}
                }
                """,
                """
                {
                  "message": "The @context directive on type 'Result' in schema 'A' has the invalid name '_result'. A context name must start with a letter and contain only letters and digits.",
                  "code": "CONTEXT_NAME_INVALID",
                  "severity": "Error",
                  "coordinate": "Result",
                  "member": "context",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }
}
