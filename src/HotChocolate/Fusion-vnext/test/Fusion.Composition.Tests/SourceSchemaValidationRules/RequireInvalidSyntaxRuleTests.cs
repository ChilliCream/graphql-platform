namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RequireInvalidSyntaxRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RequireInvalidSyntaxRule();

    // In the following example, the @require directive’s "field" argument is a valid selection map
    // and satisfies the rule.
    [Fact]
    public void Validate_RequireValidSyntax_Succeeds()
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

    // In the following example, the @require directive’s "field" argument has invalid syntax
    // because it is missing a closing brace.
    [Fact]
    public void Validate_RequireInvalidSyntax_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") {
                    id: ID!
                    profile(name: String! @require(field: "{ name ")): Profile
                }

                type Profile {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                "The @require directive on argument 'User.profile(name:)' in schema 'A' contains "
                + "invalid syntax in the 'field' argument."
            ]);
    }
}
