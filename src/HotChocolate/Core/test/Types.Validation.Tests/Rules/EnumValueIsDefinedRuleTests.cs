using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class EnumValueIsDefinedRuleTests : RuleTestBase<EnumValueIsDefinedRule>
{
    [Fact]
    public void Validate_EnumValueIsDefined_Succeeds()
    {
        AssertValid(
            """
            type FooObject @foo(arg: VALUE) {
                field(arg: FooEnum = VALUE): Int
            }

            input FooInput {
                field: FooEnum = VALUE
            }

            directive @foo(arg: FooEnum = VALUE) on OBJECT

            enum FooEnum {
                VALUE
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentDefaultEnumValueIsUndefined_Fails()
    {
        AssertInvalid(
            """
            type FooObject {
                field(arg: FooEnum = MISSING): Int
            }

            enum FooEnum {
                VALUE
            }
            """,
            """
            {
                "message": "The default value 'MISSING' of argument 'FooObject.field(arg:)' is not defined in the enum 'FooEnum'.",
                "code": "HCV0023",
                "severity": "Error",
                "coordinate": "FooObject.field(arg:)",
                "member": "arg",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveArgumentDefaultEnumValueIsUndefined_Fails()
    {
        AssertInvalid(
            """
            directive @foo(arg: FooEnum = MISSING) on OBJECT

            enum FooEnum {
                VALUE
            }
            """,
            """
            {
                "message": "The default value 'MISSING' of argument '@foo(arg:)' is not defined in the enum 'FooEnum'.",
                "code": "HCV0023",
                "severity": "Error",
                "coordinate": "@foo(arg:)",
                "member": "arg",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_InputFieldDefaultEnumValueIsUndefined_Fails()
    {
        AssertInvalid(
            """
            input FooInput {
                field: FooEnum = MISSING
            }

            enum FooEnum {
                VALUE
            }
            """,
            """
            {
                "message": "The default value 'MISSING' of field 'FooInput.field' is not defined in the enum 'FooEnum'.",
                "code": "HCV0024",
                "severity": "Error",
                "coordinate": "FooInput.field",
                "member": "field",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentAssignedEnumValueIsUndefined_Fails()
    {
        AssertInvalid(
            """
            type FooObject @foo(arg: MISSING) {
                field: Int
            }

            directive @foo(arg: FooEnum) on OBJECT

            enum FooEnum {
                VALUE
            }
            """,
            """
            {
                "message": "The assigned value 'MISSING' of argument 'arg' on directive '@foo' is not defined in the enum 'FooEnum'.",
                "code": "HCV0025",
                "severity": "Error",
                "coordinate": "FooObject",
                "member": "FooObject",
                "extensions": {}
            }
            """);
    }
}
