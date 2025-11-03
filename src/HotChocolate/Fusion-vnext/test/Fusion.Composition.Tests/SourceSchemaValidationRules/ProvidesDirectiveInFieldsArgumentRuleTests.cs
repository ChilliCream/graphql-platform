namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesDirectiveInFieldsArgumentRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesDirectiveInFieldsArgumentRule();

    // In this example, the "fields" argument of the @provides directive does not have any directive
    // applications, satisfying the rule.
    [Fact]
    public void Validate_ProvidesNoDirectiveInFieldsArgument_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id name") {
                id: ID!
                name: String
                profile: Profile @provides(fields: "name")
            }

            type Profile {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // In this example, the "fields" argument of the @provides directive has a directive application
    // @lowercase, which is not allowed.
    [Fact]
    public void Validate_ProvidesDirectiveInFieldsArgument_Fails()
    {
        AssertInvalid(
            [
                """
                directive @lowercase on FIELD_DEFINITION

                type User @key(fields: "id name") {
                    id: ID!
                    name: String
                    profile: Profile @provides(fields: "name @lowercase")
                }

                type Profile {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'User.profile' in schema 'A' references field 'name', which must not include directive applications.",
                    "code": "PROVIDES_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User.profile",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Nested field.
    [Fact]
    public void Validate_ProvidesDirectiveInFieldsArgumentNested_Fails()
    {
        AssertInvalid(
            [
                """
                directive @lowercase on FIELD_DEFINITION

                type User @key(fields: "id name") {
                    id: ID!
                    name: String
                    profile: Profile @provides(fields: "info { name @lowercase }")
                }

                type Profile {
                    id: ID!
                    info: ProfileInfo!
                }

                type ProfileInfo {
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'User.profile' in schema 'A' references field 'info.name', which must not include directive applications.",
                    "code": "PROVIDES_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User.profile",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Multiple fields.
    [Fact]
    public void Validate_ProvidesDirectiveInFieldsArgumentMultipleFields_Fails()
    {
        AssertInvalid(
            [
                """
                directive @example on FIELD_DEFINITION

                type User @key(fields: "id name") {
                    id: ID!
                    name: String
                    profile: Profile @provides(fields: "id @example name @example")
                }

                type Profile {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'User.profile' in schema 'A' references field 'id', which must not include directive applications.",
                    "code": "PROVIDES_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User.profile",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The @provides directive on field 'User.profile' in schema 'A' references field 'name', which must not include directive applications.",
                    "code": "PROVIDES_DIRECTIVE_IN_FIELDS_ARG",
                    "severity": "Error",
                    "coordinate": "User.profile",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
