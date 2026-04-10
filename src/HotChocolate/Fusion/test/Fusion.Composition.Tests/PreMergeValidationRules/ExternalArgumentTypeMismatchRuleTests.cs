namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class ExternalArgumentTypeMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalArgumentTypeMismatchRule();

    // Here, the @external field's "language" argument has the same type (Language) as the base
    // field, satisfying the rule.
    [Fact]
    public void Validate_ExternalArgumentTypeMatch_Succeeds()
    {
        AssertValid(
        [
            """
            # Source schema A
            type Product {
                name(language: Language): String
            }
            """,
            """
            # Source schema B
            type Product {
                name(language: Language): String @external
            }
            """
        ]);
    }

    // In this example, the @external field's "language" argument type does not match the base
    // field's language argument type (Language vs. String), violating the rule.
    [Fact]
    public void Validate_ExternalArgumentTypeMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                # Source schema A
                type Product {
                    name(language: Language): String
                }
                """,
                """
                # Source schema B
                type Product {
                    name(language: String): String @external
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'language' on external field 'Product.name' in schema 'B' has a different type (String) than it does in schema 'A' (Language).",
                    "code": "EXTERNAL_ARGUMENT_TYPE_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Product.name(language:)",
                    "member": "language",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
