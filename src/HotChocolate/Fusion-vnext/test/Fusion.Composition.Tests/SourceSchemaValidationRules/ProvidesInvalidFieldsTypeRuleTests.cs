namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesInvalidFieldsTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesInvalidFieldsTypeRule();

    // In this example, the @provides directive on "details" uses the string
    // "features specifications" to specify that both fields are provided in the child type
    // "ProductDetails".
    [Fact]
    public void Validate_ProvidesValidFieldsType_Succeeds()
    {
        AssertValid(
        [
            """
            type Product {
                id: ID!
                details: ProductDetails @provides(fields: "features specifications")
            }

            type ProductDetails {
                features: [String]
                specifications: String
            }

            type Query {
                products: [Product]
            }
            """
        ]);
    }

    // Here, the @provides directive includes a numeric value (123) instead of a string in its
    // "fields" argument. This invalid usage raises a PROVIDES_INVALID_FIELDS_TYPE error.
    [Fact]
    public void Validate_ProvidesInvalidFieldsType_Fails()
    {
        AssertInvalid(
            [
                """
                type Product {
                    id: ID!
                    details: ProductDetails @provides(fields: 123)
                }

                type ProductDetails {
                    features: [String]
                    specifications: String
                }
                """
            ],
            [
                "The @provides directive on field 'Product.details' in schema 'A' must specify a "
                + "string value for the 'fields' argument."
            ]);
    }
}
