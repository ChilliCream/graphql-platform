namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesInvalidFieldsRule();

    // In the following example, the @provides directive references a valid field ("hobbies") on the
    // "UserDetails" type.
    [Fact]
    public void Validate_ProvidesValidFields_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id") {
                id: ID!
                details: UserDetails @provides(fields: "hobbies")
            }

            type UserDetails {
                hobbies: [String]
            }
            """
        ]);
    }

    // In the following example, the @provides directive specifies a field named "unknownField"
    // which is not defined on "UserDetails". This raises a PROVIDES_INVALID_FIELDS error.
    [Fact]
    public void Validate_ProvidesInvalidFields_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") {
                    id: ID!
                    details: UserDetails @provides(fields: "unknownField")
                }

                type UserDetails {
                    hobbies: [String]
                }
                """
            ],
            [
                "The @provides directive on field 'User.details' in schema 'A' specifies an "
                + "invalid field selection."
            ]);
    }
}
