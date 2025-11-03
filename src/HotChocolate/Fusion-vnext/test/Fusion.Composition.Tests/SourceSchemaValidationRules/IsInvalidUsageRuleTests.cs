namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class IsInvalidUsageRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new IsInvalidUsageRule();

    // In the following example, the @is directive is applied to an argument declared on a field
    // with the @lookup directive, satisfying the rule.
    [Fact]
    public void Validate_IsValidUsage_Succeeds()
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

    // In the following example, the @is directive is applied to an argument declared on a field
    // without the @lookup directive, violating the rule.
    [Fact]
    public void Validate_IsInvalidUsage_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    personById(id: ID! @is(field: "id")): Person
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personById(id:)' in schema 'A' is invalid because the declaring field is not a lookup field.",
                    "code": "IS_INVALID_USAGE",
                    "severity": "Error",
                    "coordinate": "Query.personById(id:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
