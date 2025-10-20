namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesOnNonCompositeFieldRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesOnNonCompositeFieldRule();

    // Here, "profile" has an object base type "Profile". The @provides directive can validly
    // specify sub-fields like "settings { theme }".
    [Fact]
    public void Validate_ProvidesOnCompositeField_Succeeds()
    {
        AssertValid(
        [
            """
            type Profile {
                email: String
                settings: Settings
            }

            type Settings {
                notificationsEnabled: Boolean
                theme: String
            }

            type User {
                id: ID!
                profile: Profile @provides(fields: "settings { theme }")
            }
            """
        ]);
    }

    // In this example, "email" has a scalar base type (String). Because scalars do not expose
    // sub-fields, attaching @provides to "email" triggers a PROVIDES_ON_NON_COMPOSITE_FIELD error.
    [Fact]
    public void Validate_ProvidesOnNonCompositeFieldString_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    id: ID!
                    email: String @provides(fields: "length")
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'User.email' in schema 'A' includes a @provides directive, but does not return a composite type.",
                    "code": "PROVIDES_ON_NON_COMPOSITE_FIELD",
                    "severity": "Error",
                    "coordinate": "User.email",
                    "member": "email",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Here, the schema is defined with "email" being a non-null string.
    [Fact]
    public void Validate_ProvidesOnNonCompositeFieldNonNullString_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    id: ID!
                    email: String! @provides(fields: "length")
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'User.email' in schema 'A' includes a @provides directive, but does not return a composite type.",
                    "code": "PROVIDES_ON_NON_COMPOSITE_FIELD",
                    "severity": "Error",
                    "coordinate": "User.email",
                    "member": "email",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Here, the schema is defined with "emails" being a non-null list of non-null strings.
    [Fact]
    public void Validate_ProvidesOnNonCompositeFieldNonNullListOfNonNullStrings_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    id: ID!
                    emails: [String!]! @provides(fields: "length")
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'User.emails' in schema 'A' includes a @provides directive, but does not return a composite type.",
                    "code": "PROVIDES_ON_NON_COMPOSITE_FIELD",
                    "severity": "Error",
                    "coordinate": "User.emails",
                    "member": "emails",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
