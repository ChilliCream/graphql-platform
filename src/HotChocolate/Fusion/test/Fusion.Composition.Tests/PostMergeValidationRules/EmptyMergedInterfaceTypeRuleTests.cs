namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedInterfaceTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EmptyMergedInterfaceTypeRule();

    // In the following example, the merged object type "Product" is valid. It includes all fields
    // from both source schemas, with "price" being hidden due to the @inaccessible directive in one
    // of the source schemas.
    [Fact]
    public void Validate_NonEmptyMergedInterfaceTypeInaccessibleInterfaceField_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Product {
                name: String
                price: Int @inaccessible
            }
            """,
            """
            # Schema B
            interface Product {
                name: String
                inStock: Boolean
            }
            """
        ]);
    }

    // If the @inaccessible directive is applied to an interface type itself, the entire merged
    // interface type is excluded from the composite execution schema, and it is not required to
    // contain any fields.
    [Fact]
    public void Validate_NonEmptyMergedInterfaceTypeInaccessibleInterfaceType_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Product @inaccessible {
                name: String
                price: Int
            }
            """,
            """
            # Schema B
            interface Product {
                name: String
                inStock: Boolean
            }
            """
        ]);
    }

    // This example demonstrates an invalid merged interface type. In this case, "Product" is
    // defined in two source schemas, but all fields are marked as @inaccessible in at least one of
    // the source schemas, resulting in an empty merged interface type.
    [Fact]
    public void Validate_EmptyMergedInterfaceTypeAllFieldsInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface Product {
                    name: String
                    price: Int @inaccessible
                }
                """,
                """
                # Schema B
                interface Product {
                    name: String @inaccessible
                    price: Int
                }
                """
            ],
            [
                """
                {
                    "message": "The merged interface type 'Product' is empty.",
                    "code": "EMPTY_MERGED_INTERFACE_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "Product",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
