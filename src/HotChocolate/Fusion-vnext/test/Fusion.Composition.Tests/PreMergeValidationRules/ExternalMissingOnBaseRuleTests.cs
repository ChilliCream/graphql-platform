namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class ExternalMissingOnBaseRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalMissingOnBaseRule();

    // Here, the "name" field on "Product" is defined in source schema A and marked as @external in
    // source schema B, which is valid because there is a base definition in source schema A.
    [Fact]
    public void Validate_ExternalExistsOnBase_Succeeds()
    {
        AssertValid(
        [
            """
            # Source schema A
            type Product {
                id: ID
                name: String
            }
            """,
            """
            # Source schema B
            type Product {
                id: ID
                name: String @external
            }
            """
        ]);
    }

    // In this example, the "name" field on "Product" is marked as @external in source schema B but
    // has no non-@external declaration in any other source schema, violating the rule.
    [Fact]
    public void Validate_ExternalMissingOnBase_Fails()
    {
        AssertInvalid(
            [
                """
                # Source schema A
                type Product {
                    id: ID
                }
                """,
                """
                # Source schema B
                type Product {
                    id: ID
                    name: String @external
                }
                """
            ],
            [
                "The external field 'Product.name' in schema 'B' is not defined (non-external) in "
                + "any other schema."
            ]);
    }

    // The "name" field is marked as @external in both source schemas.
    [Fact]
    public void Validate_ExternalMissingOnBaseBothExternal_Fails()
    {
        AssertInvalid(
            [
                """
                # Source schema A
                type Product {
                    id: ID
                    name: String @external
                }
                """,
                """
                # Source schema B
                type Product {
                    id: ID
                    name: String @external
                }
                """
            ],
            [
                "The external field 'Product.name' in schema 'A' is not defined (non-external) in "
                + "any other schema.",

                "The external field 'Product.name' in schema 'B' is not defined (non-external) in "
                + "any other schema."
            ]);
    }
}
