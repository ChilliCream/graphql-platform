namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RequireInvalidFieldTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RequireInvalidFieldTypeRule();

    // In the following example, the @require directive’s "field" argument is a valid string and
    // satisfies the rule.
    [Fact]
    public void Validate_RequireValidFieldType_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id") {
                id: ID!
                profile(name: String @require(field: "name")): Profile
            }

            type Profile {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // Since "field" is set to 123 (an integer) instead of a string, this violates the rule and
    // triggers a REQUIRE_INVALID_FIELD_TYPE error.
    [Fact]
    public void Validate_RequireInvalidFieldType_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") {
                    id: ID!
                    profile(name: String! @require(field: 123)): Profile
                }

                type Profile {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                "The @require directive on argument 'User.profile(name:)' in schema 'A' must "
                + "specify a string value for the 'field' argument."
            ]);
    }
}
