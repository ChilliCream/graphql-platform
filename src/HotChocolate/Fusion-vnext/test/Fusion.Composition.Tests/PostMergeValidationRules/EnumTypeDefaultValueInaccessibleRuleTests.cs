namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EnumTypeDefaultValueInaccessibleRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EnumTypeDefaultValueInaccessibleRule();

    // In this example the "FOO" value in the "Enum1" enum is not marked with @inaccessible, hence
    // it does not violate the rule.
    [Fact]
    public void Validate_EnumTypeDefaultValueAccessible_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                field(type: Enum1 = FOO): [Baz!]!
            }

            enum Enum1 {
                FOO
                BAR
            }
            """
        ]);
    }

    // Non-nullable default values.
    [Fact]
    public void Validate_EnumTypeDefaultValueAccessibleNonNullable_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                field1(type: Enum1! = FOO): [Baz!]!
                field2(type: [Int!]! = [1]): [Baz!]!
                field3(type: Input1! = { field1: 1 }): [Baz!]!
            }

            enum Enum1 {
                FOO
                BAR
            }

            input Input1 {
                field1: Int!
            }
            """
        ]);
    }

    // The following example violates this rule because the default value for the argument (arg) and
    // the input field (field) references an enum value (FOO), that is marked as @inaccessible.
    [Fact]
    public void Validate_EnumTypeDefaultValueInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    field(arg: Enum1 = FOO): [Baz!]!
                }

                input Input1 {
                    field: Enum1 = FOO
                }

                enum Enum1 {
                    FOO @inaccessible
                    BAR
                }
                """
            ],
            [
                "The default value of 'Query.field(arg:)' references the inaccessible enum value "
                + "'Enum1.FOO'.",

                "The default value of 'Input1.field' references the inaccessible enum value "
                + "'Enum1.FOO'."
            ]);
    }

    // The following example violates this rule because the default value for the argument (arg) and
    // the input field (field2) references an @inaccessible enum value (FOO) within an object value.
    [Fact]
    public void Validate_EnumTypeDefaultValueInaccessibleObjectValueReference_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    field(arg: Input1 = { field1: FOO }): [Baz!]!
                }

                input Input1 {
                    field1: Enum1
                    field2: Input2 = { field3: FOO }
                }

                input Input2 {
                    field3: Enum1
                }

                enum Enum1 {
                    FOO @inaccessible
                    BAR
                }
                """
            ],
            [
                "The default value of 'Query.field(arg:)' references the inaccessible enum value "
                + "'Enum1.FOO'.",

                "The default value of 'Input1.field2' references the inaccessible enum value "
                + "'Enum1.FOO'."
            ]);
    }

    // The following example violates this rule because the default value for the argument (arg) and
    // the input field (field) references an @inaccessible enum value (FOO) within a list.
    [Fact]
    public void Validate_EnumTypeDefaultValueInaccessibleListReference_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    field(arg: [Enum1] = [FOO]): [Baz!]!
                }

                input Input1 {
                    field: [Enum1] = [FOO]
                }

                enum Enum1 {
                    FOO @inaccessible
                    BAR
                }
                """
            ],
            [
                "The default value of 'Query.field(arg:)' references the inaccessible enum value "
                + "'Enum1.FOO'.",

                "The default value of 'Input1.field' references the inaccessible enum value "
                + "'Enum1.FOO'."
            ]);
    }
}
