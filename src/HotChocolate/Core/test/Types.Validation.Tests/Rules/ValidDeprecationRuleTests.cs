using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class ValidDeprecationRuleTests : RuleTestBase<ValidDeprecationRule>
{
    [Fact]
    public void Validate_ValidArgumentDeprecationNullable_Succeeds()
    {
        AssertValid(
            """
            type FooObject {
                field(arg: Int @deprecated): String
            }

            interface FooInterface {
                field(arg: Int @deprecated): String
            }

            directive @foo(arg: Int @deprecated) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Validate_ValidArgumentDeprecationWithDefault_Succeeds()
    {
        AssertValid(
            """
            type FooObject {
                field(arg: Int! = 0 @deprecated): String
            }

            interface FooInterface {
                field(arg: Int! = 0 @deprecated): String
            }

            directive @foo(arg: Int! = 0 @deprecated) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Validate_ValidInputFieldDeprecationNullable_Succeeds()
    {
        AssertValid(
            """
            input FooInput {
                field: Int @deprecated
            }
            """);
    }

    [Fact]
    public void Validate_ValidInputFieldDeprecationWithDefault_Succeeds()
    {
        AssertValid(
            """
            input FooInput {
                field: Int! = 0 @deprecated
            }
            """);
    }

    [Fact]
    public void Validate_InvalidObjectFieldArgumentDeprecation_Fails()
    {
        AssertInvalid(
            """
            type FooObject {
                field(arg: Int! @deprecated): String
            }
            """,
            """
            {
                "message": "The required argument 'FooObject.field(arg:)' cannot be deprecated.",
                "code": "HCV0003",
                "severity": "Error",
                "coordinate": "FooObject.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidInterfaceFieldArgumentDeprecation_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface {
                field(arg: Int! @deprecated): String
            }
            """,
            """
            {
                "message": "The required argument 'FooInterface.field(arg:)' cannot be deprecated.",
                "code": "HCV0003",
                "severity": "Error",
                "coordinate": "FooInterface.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidDirectiveArgumentDeprecation_Fails()
    {
        AssertInvalid(
            "directive @foo(arg: Int! @deprecated) on FIELD_DEFINITION",
            """
            {
                "message": "The required argument '@foo(arg:)' cannot be deprecated.",
                "code": "HCV0003",
                "severity": "Error",
                "coordinate": "@foo(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidInputFieldDeprecation_Fails()
    {
        AssertInvalid(
            """
            input FooInput {
                field: Int! @deprecated
            }
            """,
            """
            {
                "message": "The required Input Object field 'FooInput.field' cannot be deprecated.",
                "code": "HCV0004",
                "severity": "Error",
                "coordinate": "FooInput.field",
                "member": "field",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }
}
