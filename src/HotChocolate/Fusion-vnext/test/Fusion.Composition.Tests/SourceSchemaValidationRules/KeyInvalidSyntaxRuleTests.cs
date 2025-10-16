namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyInvalidSyntaxRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new KeyInvalidSyntaxRule();

    // In this example, the "fields" argument is a correctly formed selection set:
    // "sku featuredItem { id }" is properly balanced and contains no syntax errors.
    [Fact]
    public void Validate_KeyValidSyntax_Succeeds()
    {
        AssertValid(
        [
            """
            type Product @key(fields: "sku featuredItem { id }") {
                sku: String!
                featuredItem: Node!
            }

            interface Node {
                id: ID!
            }
            """
        ]);
    }

    // Here, the selection set "featuredItem { id" is missing the closing brace "}". It is thus
    // invalid syntax, causing a KEY_INVALID_SYNTAX error.
    [Fact]
    public void Validate_KeyInvalidSyntax_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "featuredItem { id") {
                    featuredItem: Node!
                    sku: String!
                }

                interface Node {
                    id: ID!
                }
                """
            ],
            [
                "A @key directive on type 'Product' in schema 'A' contains invalid syntax in the "
                + "'fields' argument."
            ]);
    }
}
