namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyDirectiveInFieldsArgumentRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new KeyDirectiveInFieldsArgumentRule();

    // In this example, the "fields" argument of the @key directive does not include any directive
    // applications, satisfying the rule.
    [Fact]
    public void Validate_KeyNoDirectiveInFieldsArgument_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id name") {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // In this example, the "fields" argument of the @key directive includes a directive application
    // @lowercase, which is not allowed.
    [Fact]
    public void Validate_KeyDirectiveInFieldsArgument_Fails()
    {
        AssertInvalid(
            [
                """
                directive @lowercase on FIELD_DEFINITION

                type User @key(fields: "id name @lowercase") {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'name', which must not include directive applications.",
                    "code": "KEY_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // In this example, the "fields" argument includes a directive application @lowercase nested
    // inside the selection set, which is also invalid.
    [Fact]
    public void Validate_KeyDirectiveInFieldsArgumentNested_Fails()
    {
        AssertInvalid(
            [
                """
                directive @lowercase on FIELD_DEFINITION

                type User @key(fields: "id name { firstName @lowercase }") {
                    id: ID!
                    name: FullName
                }

                type FullName {
                    firstName: String
                    lastName: String
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'name.firstName', which must not include directive applications.",
                    "code": "KEY_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Multiple keys.
    [Fact]
    public void Validate_KeyDirectiveInFieldsArgumentMultipleKeys_Fails()
    {
        AssertInvalid(
            [
                """
                directive @example on FIELD_DEFINITION

                type User @key(fields: "id @example") @key(fields: "name @example") {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'id', which must not include directive applications.",
                    "code": "KEY_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'name', which must not include directive applications.",
                    "code": "KEY_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
