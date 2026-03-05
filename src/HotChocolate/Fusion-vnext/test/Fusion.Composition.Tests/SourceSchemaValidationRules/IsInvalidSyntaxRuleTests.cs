namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class IsInvalidSyntaxRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new IsInvalidSyntaxRule();

    // In the following example, the @is directive’s "field" argument is a valid FieldSelectionMap
    // and satisfies the rule.
    [Fact]
    public void Validate_IsValidSyntax_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                product(id: ID! @is(field: "id")): Product @lookup
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // In the following example, the @is directive’s "field" argument has invalid syntax because it
    // is missing a closing brace.
    [Fact]
    public void Validate_IsInvalidSyntax_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    product(id: ID! @is(field: "{ id ")): Product @lookup
                }

                type Product {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.product(id:)' in schema 'A' contains invalid syntax in the 'field' argument.",
                    "code": "IS_INVALID_SYNTAX",
                    "severity": "Error",
                    "coordinate": "Query.product(id:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
