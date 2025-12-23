using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveIsDefinedRuleTests : RuleTestBase<DirectiveIsDefinedRule>
{
    [Fact]
    public void Validate_DirectiveIsDefined_Succeeds()
    {
        AssertValid(
            """
            type Object @example {
                field(argument: Int @example): Int @example
            }

            directive @example on ARGUMENT_DEFINITION | FIELD_DEFINITION | OBJECT
            """);
    }

    [Fact]
    public void Validate_DirectiveIsSpecDirective_Succeeds()
    {
        AssertValid(
            """
            type Object {
                field: Int @deprecated
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveIsUndefined_Fails()
    {
        AssertInvalid(
            """
            type Object @example {
                field(argument: Int @example): Int @example
            }
            """,
            """
            {
                "message": "The directive '@example' on 'Object' is not defined in the schema.",
                "code": "HCV0026",
                "severity": "Error",
                "coordinate": "Object",
                "member": "Object",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The directive '@example' on 'Object.field(argument:)' is not defined in the schema.",
                "code": "HCV0026",
                "severity": "Error",
                "coordinate": "Object.field(argument:)",
                "member": "argument",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The directive '@example' on 'Object.field' is not defined in the schema.",
                "code": "HCV0026",
                "severity": "Error",
                "coordinate": "Object.field",
                "member": "field",
                "extensions": {}
            }
            """);
    }
}
