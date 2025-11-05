namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InvalidFieldSharingRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InvalidFieldSharingRule();

    // In this example, the "User" type field "fullName" is marked as shareable in both schemas,
    // allowing them to serve consistent data for that field without conflict.
    [Fact]
    public void Validate_ValidFieldSharing_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type User @key(fields: "id") {
                id: ID!
                username: String
                fullName: String @shareable
            }
            """,
            """
            # Schema B
            type User @key(fields: "id") {
                id: ID!
                fullName: String @shareable
                email: String
            }
            """
        ]);
    }

    // In the following example, "User.fullName" is overridden in one schema and therefore the field
    // can be defined in the other schema without being marked as @shareable.
    [Fact]
    public void Validate_ValidFieldSharingOneDefinitionOverridden_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type User @key(fields: "id") {
                id: ID!
                fullName: String @override(from: "B")
            }
            """,
            """
            # Schema B
            type User @key(fields: "id") {
                id: ID!
                fullName: String
            }
            """
        ]);
    }

    // In the following example, "User.fullName" is marked as @external in one schema and therefore
    // the field can be defined in the other schema without being marked as @shareable.
    [Fact] public void Validate_ValidFieldSharingOneDefinitionExternal_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type User @key(fields: "id") {
                id: ID!
                fullName: String @external
            }
            """,
            """
            # Schema B
            type User @key(fields: "id") {
                id: ID!
                fullName: String
            }
            """
        ]);
    }

    // In the following example, "User.fullName" is non-shareable but is defined and resolved by two
    // different schemas, resulting in an INVALID_FIELD_SHARING error.
    [Fact]
    public void Validate_InvalidFieldSharing_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type User @key(fields: "id") {
                    id: ID!
                    fullName: String
                }
                """,
                """
                # Schema B
                type User @key(fields: "id") {
                    id: ID!
                    fullName: String
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'User.fullName' in schema 'A' must be shareable.",
                    "code": "INVALID_FIELD_SHARING",
                    "severity": "Error",
                    "coordinate": "User.fullName",
                    "member": "fullName",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The field 'User.fullName' in schema 'B' must be shareable.",
                    "code": "INVALID_FIELD_SHARING",
                    "severity": "Error",
                    "coordinate": "User.fullName",
                    "member": "fullName",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
