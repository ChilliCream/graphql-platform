namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class KeyInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new KeyInvalidFieldsRule();

    // In this example, the "fields" argument of the @key directive is properly defined with valid
    // syntax and references existing fields.
    [Fact]
    public void Validate_KeyValidFields_Succeeds()
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

    // In this example, the "fields" argument references a field from another source schema.
    [Fact]
    public void Validate_KeyValidFieldsAcrossSchemas_Succeeds()
    {
        AssertValid(
        [
            """
            type Product @key(fields: "sku featuredItem { id }") {
                id: ID!
                sku: String!
            }
            """,
            """
            type Query {
                node(id: ID!): Node
            }

            type Product {
                featuredItem: Node!
            }

            interface Node {
                id: ID!
            }
            """
        ]);
    }

    // In this example, the "fields" argument of the @key directive references a field "id", which
    // does not exist on the "Product" type.
    [Fact]
    public void Validate_KeyInvalidFields_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "id") {
                    sku: String!
                }
                """
            ],
            [
                """
                A @key directive on type 'Product' in schema 'A' specifies an invalid field selection against the composed schema.
                - The field 'id' does not exist on the type 'Product'.
                """
            ]);
    }

    // Two errors.
    [Fact]
    public void Validate_KeyInvalidFieldsTwoErrors_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "id name") {
                    sku: String!
                }
                """
            ],
            [
                """
                A @key directive on type 'Product' in schema 'A' specifies an invalid field selection against the composed schema.
                - The field 'id' does not exist on the type 'Product'.
                - The field 'name' does not exist on the type 'Product'.
                """
            ]);
    }
}
