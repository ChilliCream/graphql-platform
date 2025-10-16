namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedObjectTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EmptyMergedObjectTypeRule();

    // In the following example, the merged object type "Author" is valid. It includes all fields
    // from both source schemas, with "age" being hidden due to the @inaccessible directive in one
    // of the source schemas.
    [Fact]
    public void Validate_NonEmptyMergedObjectTypeInaccessibleObjectField_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Author {
                name: String
                age: Int @inaccessible
            }
            """,
            """
            # Schema B
            type Author {
                age: Int
                registered: Boolean
            }
            """
        ]);
    }

    // If the @inaccessible directive is applied to an object type itself, the entire merged object
    // type is excluded from the composite execution schema, and it is not required to contain any
    // fields.
    [Fact]
    public void Validate_NonEmptyMergedObjectTypeInaccessibleObjectType_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Author @inaccessible {
                name: String
                age: Int
            }
            """,
            """
            # Schema B
            type Author {
                registered: Boolean
            }
            """
        ]);
    }

    // The rule does not apply to root operation types.
    [Fact]
    public void Validate_NonEmptyMergedObjectTypeRootOperationType_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                field: Int @inaccessible
            }

            type Mutation {
                field: Int @inaccessible
            }

            type Subscription {
                field: Int @inaccessible
            }
            """
        ]);
    }

    // This example demonstrates an invalid merged object type. In this case, "Author" is defined in
    // two source schemas, but all fields are marked as @inaccessible in at least one of the source
    // schemas, resulting in an empty merged object type.
    [Fact]
    public void Validate_EmptyMergedObjectTypeAllFieldsInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Author {
                    name: String @inaccessible
                    registered: Boolean
                }
                """,
                """
                # Schema B
                type Author {
                    name: String
                    registered: Boolean @inaccessible
                }
                """
            ],
            [
                "The merged object type 'Author' is empty."
            ]);
    }
}
