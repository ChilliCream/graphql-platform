namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyFieldsHasArgumentsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new KeyFieldsHasArgumentsRule();

    // In this example, the "User" type has a valid @key directive that references the argument-free
    // fields "id" and "name".
    [Fact]
    public void Validate_KeyFieldsHasNoArguments_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id name") {
                id: ID!
                name: String
                tags: [String]
            }
            """
        ]);
    }

    // In this example, the @key directive references a field ("tags") that is defined with
    // arguments ("limit"), which is not allowed.
    [Fact]
    public void Validate_KeyFieldsHasArguments_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id tags") {
                    id: ID!
                    tags(limit: Int = 10): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'User.tags', which must not have arguments.",
                    "code": "KEY_FIELDS_HAS_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Nested field.
    [Fact]
    public void Validate_KeyFieldsHasArgumentsNested_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id info { tags }") {
                    id: ID!
                    info: UserInfo
                }

                type UserInfo {
                    tags(limit: Int = 10): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'UserInfo.tags', which must not have arguments.",
                    "code": "KEY_FIELDS_HAS_ARGUMENTS",
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
    public void Validate_KeyFieldsHasArgumentsMultipleKeys_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") @key(fields: "tags") {
                    id(global: Boolean = true): ID!
                    tags(limit: Int = 10): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'User.id', which must not have arguments.",
                    "code": "KEY_FIELDS_HAS_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' references field 'User.tags', which must not have arguments.",
                    "code": "KEY_FIELDS_HAS_ARGUMENTS",
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
