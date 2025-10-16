namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedEnumTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EmptyMergedEnumTypeRule();

    // In the following example, the merged enum type "DeliveryStatus" is valid. It includes all
    // values from both source schemas, with PENDING being hidden due to the @inaccessible directive
    // in one of the source schemas.
    [Fact]
    public void Validate_NonEmptyMergedEnumTypeInaccessibleEnumValue_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            enum DeliveryStatus {
                PENDING @inaccessible
                SHIPPED
                DELIVERED
            }
            """,
            """
            # Schema B
            enum DeliveryStatus {
                SHIPPED
                DELIVERED
            }
            """
        ]);
    }

    // If the @inaccessible directive is applied to an enum type itself, the entire merged enum type
    // is excluded from the composite execution schema, and it is not required to contain any
    // values.
    [Fact]
    public void Validate_NonEmptyMergedEnumTypeInaccessibleEnumType_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            enum DeliveryStatus @inaccessible {
                SHIPPED
                DELIVERED
            }
            """,
            """
            # Schema B
            enum DeliveryStatus {
                SHIPPED
                DELIVERED
            }
            """
        ]);
    }

    // This example demonstrates an invalid merged enum type. In this case, "DeliveryStatus" is
    // defined in two source schemas, but all values are marked as @inaccessible in at least one of
    // the source schemas, resulting in an empty merged enum type.
    [Fact]
    public void Validate_EmptyMergedEnumTypeAllEnumValuesInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                enum DeliveryStatus {
                    PENDING @inaccessible
                    DELIVERED
                }
                """,
                """
                # Schema B
                enum DeliveryStatus {
                    PENDING
                    DELIVERED @inaccessible
                }
                """
            ],
            [
                "The merged enum type 'DeliveryStatus' is empty."
            ]);
    }
}
