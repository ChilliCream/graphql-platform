namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class ExternalArgumentMissingRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalArgumentMissingRule();

    // In this example, the "language" argument is present on both the @external field in source
    // schema B and the base field in source schema A, satisfying the rule.
    [Fact]
    public void Validate_ExternalArgumentNotMissing_Succeeds()
    {
        AssertValid(
        [
            """
            # Source schema A
            type Product {
                name(language: String): String
            }
            """,
            """
            # Source schema B
            type Product {
                name(language: String): String @external
            }
            """
        ]);
    }

    // Here, the @external field in source schema B is missing the "language" argument that is
    // present in the base field definition in source schema A, violating the rule.
    [Fact]
    public void Validate_ExternalArgumentMissing_Fails()
    {
        AssertInvalid(
            [
                """
                # Source schema A
                type Product {
                    name(language: String): String
                }
                """,
                """
                # Source schema B
                type Product {
                    name: String @external
                }
                """
            ],
            [
                """
                {
                    "message": "The external field 'Product.name' in schema 'B' must define the argument 'language'.",
                    "code": "EXTERNAL_ARGUMENT_MISSING",
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
