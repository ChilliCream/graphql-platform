namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedInputObjectTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EmptyMergedInputObjectTypeRule();

    // In the following example, the merged input object type "BookFilter" is valid.
    [Fact]
    public void Validate_NonEmptyMergedInputObjectType_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                name: String
            }
            """,
            """
            # Schema B
            input BookFilter {
                name: String
            }
            """
        ]);
    }

    // If the @inaccessible directive is applied to an input object type itself, the entire merged
    // input object type is excluded from the composite schema, and it is not required to contain
    // any fields.
    [Fact]
    public void Validate_NonEmptyMergedInputObjectTypeInaccessibleInputObjectType_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter @inaccessible {
                name: String
                minPageCount: Int
            }
            """,
            """
            # Schema B
            input BookFilter {
                name: String
            }
            """
        ]);
    }

    // This example demonstrates an invalid merged input object type. In this case, "BookFilter" is
    // defined in two source schemas, but all fields are marked as @inaccessible in at least one of
    // the source schemas, resulting in an empty merged input object type.
    [Fact]
    public void Validate_EmptyMergedInputObjectTypeAllFieldsInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    name: String @inaccessible
                    paperback: Boolean
                }
                """,
                """
                # Schema B
                input BookFilter {
                    name: String
                    paperback: Boolean @inaccessible
                }
                """
            ],
            [
                "The merged input object type 'BookFilter' is empty."
            ]);
    }

    // This example demonstrates where the merged input object type is empty because no fields
    // intersect between the two source schemas.
    [Fact]
    public void Validate_EmptyMergedInputObjectTypeNoIntersectingFields_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    paperback: Boolean
                }
                """,
                """
                # Schema B
                input BookFilter {
                    name: String
                }
                """
            ],
            [
                "The merged input object type 'BookFilter' is empty."
            ]);
    }
}
