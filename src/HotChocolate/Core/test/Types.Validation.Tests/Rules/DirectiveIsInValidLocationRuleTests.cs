using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveIsInValidLocationRuleTests : RuleTestBase<DirectiveIsInValidLocationRule>
{
    [Fact]
    public void Validate_DirectiveAppliedAtDeclaredLocations_Succeeds()
    {
        AssertValid(
            """
            schema @example {
                query: Query
            }

            type Query @example {
                field(argument: Int @example): Int @example
            }

            scalar Scalar @example

            interface Interface @example {
                field: Int
            }

            union Union @example = Query

            enum Enum @example {
                VALUE @example
            }

            input Input @example {
                field: Int @example
            }

            directive @other(argument: Int @example) @example on OBJECT

            directive @example on ARGUMENT_DEFINITION
                | DIRECTIVE_DEFINITION
                | ENUM
                | ENUM_VALUE
                | FIELD_DEFINITION
                | INPUT_FIELD_DEFINITION
                | INPUT_OBJECT
                | INTERFACE
                | OBJECT
                | SCALAR
                | SCHEMA
                | UNION
            """);
    }

    [Fact]
    public void Validate_RepeatableDirectiveAppliedAtDeclaredLocation_Succeeds()
    {
        AssertValid(
            """
            type Object @example @example {
                field: Int
            }

            directive @example repeatable on OBJECT
            """);
    }

    [Fact]
    public void Validate_DirectiveDefinitionWithoutLocations_Succeeds()
    {
        // DirectiveDefinitionIncludesLocationRule reports the definition itself.
        AssertValid(
            """
            type Object @example {
                field: Int
            }

            directive @example on OBJECT
            """,
            schema => schema.DirectiveDefinitions["example"].Locations = 0);
    }

    [Fact]
    public void Validate_UndefinedDirective_Succeeds()
    {
        AssertValid(
            """
            type Object @example {
                field: Int
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnSchema_Fails()
    {
        AssertInvalid(
            """
            schema @example {
                query: Query
            }

            type Query {
                field: Int
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'schema' is not allowed on the location 'SCHEMA'.",
                "code": "HCV0032",
                "severity": "Error",
                "member": "default",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnScalar_Fails()
    {
        AssertInvalid(
            """
            scalar Scalar @example

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Scalar' is not allowed on the location 'SCALAR'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Scalar",
                "member": "Scalar",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnObject_Fails()
    {
        AssertInvalid(
            """
            type Object @example {
                field: Int
            }

            directive @example on FIELD_DEFINITION
            """,
            """
            {
                "message": "The directive '@example' on 'Object' is not allowed on the location 'OBJECT'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Object",
                "member": "Object",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnFieldDefinition_Fails()
    {
        AssertInvalid(
            """
            type Object {
                field: Int @example
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Object.field' is not allowed on the location 'FIELD_DEFINITION'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Object.field",
                "member": "field",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnArgumentDefinition_Fails()
    {
        AssertInvalid(
            """
            type Object {
                field(argument: Int @example): Int
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Object.field(argument:)' is not allowed on the location 'ARGUMENT_DEFINITION'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Object.field(argument:)",
                "member": "argument",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnInterface_Fails()
    {
        AssertInvalid(
            """
            interface Interface @example {
                field: Int
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Interface' is not allowed on the location 'INTERFACE'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Interface",
                "member": "Interface",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnUnion_Fails()
    {
        AssertInvalid(
            """
            type Object {
                field: Int
            }

            union Union @example = Object

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Union' is not allowed on the location 'UNION'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Union",
                "member": "Union",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnEnum_Fails()
    {
        AssertInvalid(
            """
            enum Enum @example {
                VALUE
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Enum' is not allowed on the location 'ENUM'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Enum",
                "member": "Enum",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnEnumValue_Fails()
    {
        AssertInvalid(
            """
            enum Enum {
                VALUE @example
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Enum.VALUE' is not allowed on the location 'ENUM_VALUE'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Enum.VALUE",
                "member": "VALUE",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnInputObject_Fails()
    {
        AssertInvalid(
            """
            input Input @example {
                field: Int
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Input' is not allowed on the location 'INPUT_OBJECT'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Input",
                "member": "Input",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnInputFieldDefinition_Fails()
    {
        AssertInvalid(
            """
            input Input {
                field: Int @example
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on 'Input.field' is not allowed on the location 'INPUT_FIELD_DEFINITION'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "Input.field",
                "member": "field",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnDirectiveDefinition_Fails()
    {
        AssertInvalid(
            """
            directive @other @example on OBJECT

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on '@other' is not allowed on the location 'DIRECTIVE_DEFINITION'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "@other",
                "member": "@other",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveNotDeclaredOnDirectiveArgumentDefinition_Fails()
    {
        AssertInvalid(
            """
            directive @other(argument: Int @example) on OBJECT

            directive @example on OBJECT
            """,
            """
            {
                "message": "The directive '@example' on '@other(argument:)' is not allowed on the location 'ARGUMENT_DEFINITION'.",
                "code": "HCV0032",
                "severity": "Error",
                "coordinate": "@other(argument:)",
                "member": "argument",
                "extensions": {}
            }
            """);
    }
}
