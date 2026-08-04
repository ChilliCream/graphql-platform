namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class ExternalTypeMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalTypeMismatchRule();

    // Here, the @external field "name" has the same return type (String) as the base field
    // definition, satisfying the rule.
    [Fact]
    public void Validate_ExternalTypeMatch_Succeeds()
    {
        AssertValid(
        [
            """
            # Source schema A
            type Product {
                name: String
            }
            """,
            """
            # Source schema B
            type Product {
                name: String @external
            }
            """
        ]);
    }

    // In this example, the @external field "name" has a return type of "ProductName" that doesn't
    // match the base field's return type "String", violating the rule.
    [Fact]
    public void Validate_ExternalTypeMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                # Source schema A
                type Product {
                    name: String
                }
                """,
                """
                # Source schema B
                type Product {
                    name: ProductName @external
                }
                """
            ],
            [
                """
                {
                    "message": "The external field 'Product.name' in schema 'B' has a different type (ProductName) than it does in schema 'A' (String).",
                    "code": "EXTERNAL_TYPE_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Product.name",
                    "member": "name",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
