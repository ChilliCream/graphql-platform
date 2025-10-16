namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesInvalidSyntaxRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesInvalidSyntaxRule();

    // Here, the @provides directive’s "fields" argument is a valid selection set.
    [Fact]
    public void Validate_ProvidesValidSyntax_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id") {
                id: ID!
                address: Address @provides(fields: "street city")
            }

            type Address {
                street: String
                city: String
            }
            """
        ]);
    }

    // In this example, the "fields" argument is missing a closing brace. It cannot be parsed as a
    // valid GraphQL selection set, triggering a PROVIDES_INVALID_SYNTAX error.
    [Fact]
    public void Validate_ProvidesInvalidSyntax_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") {
                    id: ID!
                    address: Address @provides(fields: "{ street city ")
                }
                """
            ],
            [
                "The @provides directive on field 'User.address' in schema 'A' contains invalid "
                + "syntax in the 'fields' argument."
            ]);
    }
}
