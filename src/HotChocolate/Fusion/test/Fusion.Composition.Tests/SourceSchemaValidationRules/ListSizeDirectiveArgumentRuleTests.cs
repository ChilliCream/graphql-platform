namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ListSizeDirectiveArgumentRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ListSizeDirectiveArgumentRule();

    [Fact]
    public void Validate_Should_Fail_When_CompatibleDefinitionHasNegativeAssumedSize()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(assumedSize: -1)
                }

                directive @listSize(assumedSize: Int) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'assumedSize' of the @listSize directive on field 'Query.field' in schema 'A' must not be negative (-1).",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_IncompatibleDefinitionHasNegativeAssumedSize()
    {
        AssertValid(
        [
            """
            type Query {
                field: [Int] @listSize(assumedSize: -1)
            }

            directive @listSize(
                assumedSize: Int
                custom: Boolean
            ) on FIELD_DEFINITION
            """
        ]);
    }
}
