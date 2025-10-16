namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class IsInvalidFieldTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new IsInvalidFieldTypeRule();

    // In the following example, the @is directive’s "field" argument is a valid string and
    // satisfies the rule.
    [Fact]
    public void Validate_IsValidFieldType_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                personById(id: ID! @is(field: "id")): Person @lookup
            }

            type Person {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // Since "field" is set to 123 (an integer) instead of a string, this violates the rule and
    // triggers an IS_INVALID_FIELD_TYPE error.
    [Fact]
    public void Validate_IsInvalidFieldType_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    personById(id: ID! @is(field: 123)): Person @lookup
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                "The @is directive on argument 'Query.personById(id:)' in schema 'A' must specify "
                + "a string value for the 'field' argument."
            ]);
    }
}
