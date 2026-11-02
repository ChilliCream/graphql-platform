namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class ReferenceToDeprecatedTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ReferenceToDeprecatedTypeRule();

    // A valid case where a field references a type that is not deprecated.
    [Fact]
    public void Validate_ReferenceToNonDeprecatedType_Succeeds()
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

    // Another valid case is where the referencing field is deprecated as well. The type is
    // deprecated in the first source schema and the field is deprecated in the second one.
    [Fact]
    public void Validate_ReferenceToDeprecatedTypeFromDeprecatedField_Succeeds()
    {
        AssertValid(
        [
            """
            type Object1 {
                field1: String!
            }

            type Object2 @deprecated(reason: "Use Object3.") {
                field3: String
            }
            """,
            """
            type Object1 {
                field2: Object2 @deprecated(reason: "Use Object3.")
            }

            type Object2 {
                field3: String
            }
            """
        ]);
    }

    // An invalid case is when merging pairs a deprecated type with a field that is not deprecated.
    // The type is deprecated in the first source schema, while the second source schema declares a
    // field returning it that is not deprecated.
    [Fact]
    public void Validate_ReferenceToDeprecatedTypeFromNonDeprecatedField_Fails()
    {
        AssertInvalid(
            [
                """
                type Object1 {
                    field1: String!
                }

                type Object2 @deprecated(reason: "Use Object3.") {
                    field3: String
                }
                """,
                """
                type Object1 {
                    field2: Object2
                }

                type Object2 {
                    field3: String
                }
                """
            ],
            [
                """
                {
                    "message": "The merged field 'field2' in type 'Object1' cannot reference the deprecated type 'Object2'. Either deprecate the field or change its return type.",
                    "code": "REFERENCE_TO_DEPRECATED_TYPE",
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
