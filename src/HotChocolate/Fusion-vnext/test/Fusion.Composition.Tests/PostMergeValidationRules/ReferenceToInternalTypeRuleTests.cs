namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class ReferenceToInternalTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ReferenceToInternalTypeRule();

    // A valid case where a public field references another public type.
    [Fact]
    public void Validate_ReferenceToNonInternalType_Succeeds()
    {
        AssertValid(
        [
            """
            type Object1 {
                field1: String!
                field2: Object2
            }

            type Object2 {
                field3: String
            }
            """
        ]);
    }

    // Another valid case is where the field is internal in the source schema.
    [Fact]
    public void Validate_ReferenceToInternalTypeFromInternalField_Succeeds()
    {
        AssertValid(
        [
            """
            type Object1 {
                field1: String!
                field2: Object2 @internal
            }

            type Object2 @internal {
                field3: String
            }
            """
        ]);
    }

    // An invalid case is when a field references an internal type.
    [Fact]
    public void Validate_ReferenceToInternalType_Fails()
    {
        AssertInvalid(
            [
                """
                type Object1 {
                    field1: String!
                    field2: Object2!
                }

                type Object2 @internal {
                    field3: String
                }
                """
            ],
            [
                """
                {
                    "message": "The merged field 'field2' in type 'Object1' cannot reference the internal type 'Object2'.",
                    "code": "REFERENCE_TO_INTERNAL_TYPE",
                    "severity": "Error",
                    "coordinate": "Object1.field2",
                    "member": "field2",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
