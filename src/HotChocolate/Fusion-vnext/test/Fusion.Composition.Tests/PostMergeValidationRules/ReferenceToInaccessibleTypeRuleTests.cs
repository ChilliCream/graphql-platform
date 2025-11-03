namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class ReferenceToInaccessibleTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ReferenceToInaccessibleTypeRule();

    // A valid case where a public input field references another public input type.
    [Fact]
    public void Validate_ReferenceToAccessibleTypeFromInputField_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input Input1 {
                field1: String!
                field2: Input2
            }

            input Input2 {
                field3: String
            }
            """,
            """
            # Schema B
            input Input2 {
                field3: String
            }
            """
        ]);
    }

    // A valid case where a public output field references another public output type.
    [Fact]
    public void Validate_ReferenceToAccessibleTypeFromOutputField_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Object1 {
                field1: String!
                field2: Object2
            }

            type Object2 {
                field3: String
            }
            """,
            """
            # Schema B
            type Object2 {
                field3: String
            }
            """
        ]);
    }

    // A valid case where a public field argument references another public input type.
    [Fact]
    public void Validate_ReferenceToAccessibleTypeFromFieldArgument_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Object1 {
                field1: String!
                field2(arg: Input2): String!
            }

            input Input2 {
                field3: String
            }
            """,
            """
            # Schema B
            input Input2 {
                field3: String
            }
            """
        ]);
    }

    // Another valid case is where the field is not exposed in the composed schema.
    [Fact]
    public void Validate_ReferenceToInaccessibleTypeFromInaccessibleInputField_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input Input1 {
                field1: String!
                field2: Input2 @inaccessible
            }

            input Input2 {
                field3: String
            }
            """,
            """
            # Schema B
            input Input2 @inaccessible {
                field3: String
            }
            """
        ]);
    }

    // Another valid case is where the output field is not exposed in the composed schema.
    [Fact]
    public void Validate_ReferenceToInaccessibleTypeFromInaccessibleOutputField_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Object1 {
                field1: String!
                field2: Object2 @inaccessible
            }

            type Object2 {
                field3: String
            }
            """,
            """
            # Schema B
            type Object2 @inaccessible {
                field3: String
            }
            """
        ]);
    }

    // Another valid case is where the field argument is not exposed in the composed schema.
    [Fact]
    public void Validate_ReferenceToInaccessibleTypeFromInaccessibleFieldArgument_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Object1 {
                field1: String!
                field2(arg: Input2 @inaccessible): String!
            }

            input Input2 {
                field3: String
            }
            """,
            """
            # Schema B
            input Input2 @inaccessible {
                field3: String
            }
            """
        ]);
    }

    // An invalid case is when an input field references an inaccessible type.
    [Fact] public void Validate_ReferenceToInaccessibleTypeFromInputField_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input Input1 {
                    field1: String!
                    field2: Input2!
                }

                input Input2 {
                    field3: String
                }
                """,
                """
                # Schema B
                input Input2 @inaccessible {
                    field3: String
                }
                """
            ],
            [
                """
                {
                    "message": "The merged input field 'field2' in type 'Input1' cannot reference the inaccessible type 'Input2'.",
                    "code": "REFERENCE_TO_INACCESSIBLE_TYPE",
                    "severity": "Error",
                    "coordinate": "Input1.field2",
                    "member": "field2",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }

    // An invalid case is when an output field references an inaccessible type.
    [Fact] public void Validate_ReferenceToInaccessibleTypeFromOutputField_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Object1 {
                    field1: String!
                    field2: Object2!
                }

                type Object2 {
                    field3: String
                }
                """,
                """
                # Schema B
                type Object2 @inaccessible {
                    field3: String
                }
                """
            ],
            [
                """
                {
                    "message": "The merged output field 'field2' in type 'Object1' cannot reference the inaccessible type 'Object2'.",
                    "code": "REFERENCE_TO_INACCESSIBLE_TYPE",
                    "severity": "Error",
                    "coordinate": "Object1.field2",
                    "member": "field2",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }

    // An invalid case is when a field argument references an inaccessible type.
    [Fact] public void Validate_ReferenceToInaccessibleTypeFromFieldArgument_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Object1 {
                    field1: String!
                    field2(arg: Input2!): String!
                }

                input Input2 {
                    field3: String
                }
                """,
                """
                # Schema B
                input Input2 @inaccessible {
                    field3: String
                }
                """
            ],
            [
                """
                {
                    "message": "The merged field argument 'arg' on field 'Object1.field2' cannot reference the inaccessible type 'Input2'.",
                    "code": "REFERENCE_TO_INACCESSIBLE_TYPE",
                    "severity": "Error",
                    "coordinate": "Object1.field2(arg:)",
                    "member": "arg",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
