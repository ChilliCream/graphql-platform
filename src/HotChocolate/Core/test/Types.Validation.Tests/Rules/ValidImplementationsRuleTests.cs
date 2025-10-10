using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class ValidImplementationsRuleTests : RuleTestBase<ValidImplementationsRule>
{
    [Fact]
    public void Validate_ObjectTransitivelyImplemented_Succeeds()
    {
        AssertValid(
            """
            type FooObject implements FooInterface & BarInterface {
                field1: Int
                field2: Int
            }

            interface FooInterface implements BarInterface {
                field1: Int
                field2: Int
            }

            interface BarInterface {
                field2: Int
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceTransitivelyImplemented_Succeeds()
    {
        AssertValid(
            """
            interface FooInterface implements BarInterface & BazInterface {
                field1: Int
                field2: Int
            }

            interface BarInterface implements BazInterface {
                field1: Int
                field2: Int
            }

            interface BazInterface {
                field2: Int
            }
            """);
    }

    [Fact]
    public void Validate_ObjectValidArgumentType_Succeeds()
    {
        AssertValid(
            """
            type FooObject implements FooInterface {
                field(arg: Int): Int
            }

            interface FooInterface {
                field(arg: Int): Int
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceValidArgumentType_Succeeds()
    {
        AssertValid(
            """
            interface FooInterface implements BarInterface {
                field(arg: Int): Int
            }

            interface BarInterface {
                field(arg: Int): Int
            }
            """);
    }

    [Fact]
    public void Validate_ObjectAdditionalArgumentNullable_Succeeds()
    {
        AssertValid(
            """
            type FooObject implements FooInterface {
                field(arg1: Int!, arg2: Int): Int
            }

            interface FooInterface {
                field(arg1: Int!): Int
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceAdditionalArgumentNullable_Succeeds()
    {
        AssertValid(
            """
            interface FooInterface implements BarInterface {
                field(arg1: Int!, arg2: Int): Int
            }

            interface BarInterface {
                field(arg1: Int!): Int
            }
            """);
    }

    [Fact]
    public void Validate_ObjectValidFieldType_Succeeds()
    {
        AssertValid(
            """
            type FooObject implements FooInterface {
                field: Int!
            }

            interface FooInterface {
                field: Int
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceValidFieldType_Succeeds()
    {
        AssertValid(
            """
            interface FooInterface implements BarInterface {
                field: Int!
            }

            interface BarInterface {
                field: Int
            }
            """);
    }

    [Fact]
    public void Validate_ObjectValidFieldDeprecation_Succeeds()
    {
        AssertValid(
            """
            type FooObject implements FooInterface {
                field: Int @deprecated
            }

            interface FooInterface {
                field: Int @deprecated
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceValidFieldDeprecation_Succeeds()
    {
        AssertValid(
            """
            interface FooInterface implements BarInterface {
                field: Int @deprecated
            }

            interface BarInterface {
                field: Int @deprecated
            }
            """);
    }

    [Fact]
    public void Validate_ObjectNotTransitivelyImplemented_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field1: Int
                field2: Int
            }

            interface FooInterface implements BarInterface {
                field1: Int
                field2: Int
            }

            interface BarInterface {
                field2: Int
            }
            """,
            """
            {
                "message": "The type 'FooObject' must declare all interfaces declared by implemented interfaces.",
                "code": "HCV0005",
                "severity": "Error",
                "coordinate": "FooObject",
                "member": "FooObject",
                "extensions": {
                    "implementedType": "FooInterface",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceNotTransitivelyImplemented_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field1: Int
                field2: Int
            }

            interface BarInterface implements BazInterface {
                field1: Int
                field2: Int
            }

            interface BazInterface {
                field2: Int
            }
            """,
            """
            {
                "message": "The type 'FooInterface' must declare all interfaces declared by implemented interfaces.",
                "code": "HCV0005",
                "severity": "Error",
                "coordinate": "FooInterface",
                "member": "FooInterface",
                "extensions": {
                    "implementedType": "BarInterface",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectInvalidArgumentTypeDifferentType_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field(arg: String): Int
            }

            interface FooInterface {
                field(arg: Int): Int
            }
            """,
            """
            {
                "message": "The argument 'arg' on field 'FooObject.field' must accept type 'Int' to match the argument defined in interface 'FooInterface'.",
                "code": "HCV0008",
                "severity": "Error",
                "coordinate": "FooObject.field(arg:)",
                "member": "arg",
                "extensions": {
                    "argument": "arg",
                    "implementedArgument": "arg",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceInvalidArgumentTypeDifferentType_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field(arg: String): Int
            }

            interface BarInterface {
                field(arg: Int): Int
            }
            """,
            """
            {
                "message": "The argument 'arg' on field 'FooInterface.field' must accept type 'Int' to match the argument defined in interface 'BarInterface'.",
                "code": "HCV0008",
                "severity": "Error",
                "coordinate": "FooInterface.field(arg:)",
                "member": "arg",
                "extensions": {
                    "argument": "arg",
                    "implementedArgument": "arg",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectInvalidArgumentTypeNullability_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field(arg: Int): Int
            }

            interface FooInterface {
                field(arg: Int!): Int
            }
            """,
            """
            {
                "message": "The argument 'arg' on field 'FooObject.field' must accept type 'Int!' to match the argument defined in interface 'FooInterface'.",
                "code": "HCV0008",
                "severity": "Error",
                "coordinate": "FooObject.field(arg:)",
                "member": "arg",
                "extensions": {
                    "argument": "arg",
                    "implementedArgument": "arg",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceInvalidArgumentTypeNullability_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field(arg: Int): String
            }

            interface BarInterface {
                field(arg: Int!): String
            }
            """,
            """
            {
                "message": "The argument 'arg' on field 'FooInterface.field' must accept type 'Int!' to match the argument defined in interface 'BarInterface'.",
                "code": "HCV0008",
                "severity": "Error",
                "coordinate": "FooInterface.field(arg:)",
                "member": "arg",
                "extensions": {
                    "argument": "arg",
                    "implementedArgument": "arg",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectAdditionalArgumentNotNullable_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field(arg1: Int!, arg2: Int!): Int
            }

            interface FooInterface {
                field(arg1: Int!): Int
            }
            """,
            """
            {
                "message": "The field 'FooObject.field' must only declare additional arguments to an implemented field that are nullable.",
                "code": "HCV0009",
                "severity": "Error",
                "coordinate": "FooObject.field",
                "member": "field",
                "extensions": {
                    "argument": "arg2",
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceAdditionalArgumentNotNullable_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field(arg1: Int!, arg2: Int!): Int
            }

            interface BarInterface {
                field(arg1: Int!): Int
            }
            """,
            """
            {
                "message": "The field 'FooInterface.field' must only declare additional arguments to an implemented field that are nullable.",
                "code": "HCV0009",
                "severity": "Error",
                "coordinate": "FooInterface.field",
                "member": "field",
                "extensions": {
                    "argument": "arg2",
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectArgumentNotImplemented_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field(arg1: Int): Int
            }

            interface FooInterface {
                field(arg1: Int, arg2: Int): Int
            }
            """,
            """
            {
                "message": "The field 'FooObject.field' must define the argument 'arg2' to match the field defined in interface 'FooInterface'.",
                "code": "HCV0007",
                "severity": "Error",
                "coordinate": "FooObject.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "missingArgument": "arg2",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceArgumentNotImplemented_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field(arg1: Int): Int
            }

            interface BarInterface {
                field(arg1: Int, arg2: Int): Int
            }
            """,
            """
            {
                "message": "The field 'FooInterface.field' must define the argument 'arg2' to match the field defined in interface 'BarInterface'.",
                "code": "HCV0007",
                "severity": "Error",
                "coordinate": "FooInterface.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "missingArgument": "arg2",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectInvalidFieldTypeDifferentType_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field: String
            }

            interface FooInterface {
                field: Int
            }
            """,
            """
            {
                "message": "The field 'FooObject.field' must return a type which is equal to or a sub-type of (covariant) the return type 'Int' of the interface field.",
                "code": "HCV0010",
                "severity": "Error",
                "coordinate": "FooObject.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceInvalidFieldTypeDifferentType_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field: String
            }

            interface BarInterface {
                field: Int
            }
            """,
            """
            {
                "message": "The field 'FooInterface.field' must return a type which is equal to or a sub-type of (covariant) the return type 'Int' of the interface field.",
                "code": "HCV0010",
                "severity": "Error",
                "coordinate": "FooInterface.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectInvalidFieldTypeNullability_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field: Int
            }

            interface FooInterface {
                field: Int!
            }
            """,
            """
            {
                "message": "The field 'FooObject.field' must return a type which is equal to or a sub-type of (covariant) the return type 'Int!' of the interface field.",
                "code": "HCV0010",
                "severity": "Error",
                "coordinate": "FooObject.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceInvalidFieldTypeNullability_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field: Int
            }

            interface BarInterface {
                field: Int!
            }
            """,
            """
            {
                "message": "The field 'FooInterface.field' must return a type which is equal to or a sub-type of (covariant) the return type 'Int!' of the interface field.",
                "code": "HCV0010",
                "severity": "Error",
                "coordinate": "FooInterface.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectInvalidFieldDeprecation_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field: Int @deprecated
            }

            interface FooInterface {
                field: Int
            }
            """,
            """
            {
                "message": "The field 'FooObject.field' must not be deprecated without the corresponding field in the interface 'FooInterface' being deprecated.",
                "code": "HCV0011",
                "severity": "Error",
                "coordinate": "FooObject.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceInvalidFieldDeprecation_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field: Int @deprecated
            }

            interface BarInterface {
                field: Int
            }
            """,
            """
            {
                "message": "The field 'FooInterface.field' must not be deprecated without the corresponding field in the interface 'BarInterface' being deprecated.",
                "code": "HCV0011",
                "severity": "Error",
                "coordinate": "FooInterface.field",
                "member": "field",
                "extensions": {
                    "field": "field",
                    "implementedField": "field",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ObjectFieldNotImplemented_Fails()
    {
        AssertInvalid(
            """
            type FooObject implements FooInterface {
                field1: Int
            }

            interface FooInterface {
                field1: Int
                field2: Int
            }
            """,
            """
            {
                "message": "The interface field 'FooInterface.field2' must be implemented by type 'FooObject'.",
                "code": "HCV0006",
                "severity": "Error",
                "coordinate": "FooObject",
                "member": "FooObject",
                "extensions": {
                    "implementedField": "field2",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InterfaceFieldNotImplemented_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface implements BarInterface {
                field1: Int
            }

            interface BarInterface {
                field1: Int
                field2: Int
            }
            """,
            """
            {
                "message": "The interface field 'BarInterface.field2' must be implemented by type 'FooInterface'.",
                "code": "HCV0006",
                "severity": "Error",
                "coordinate": "FooInterface",
                "member": "FooInterface",
                "extensions": {
                    "implementedField": "field2",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }
}
