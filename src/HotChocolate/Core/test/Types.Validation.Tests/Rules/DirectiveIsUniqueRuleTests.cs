using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveIsUniqueRuleTests : RuleTestBase<DirectiveIsUniqueRule>
{
    [Fact]
    public void Validate_NonRepeatableDirectiveAppliedOnce_Succeeds()
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
    public void Validate_RepeatableDirectiveAppliedTwice_Succeeds()
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
    public void Validate_NonRepeatableDirectiveAppliedTwice_Fails()
    {
        AssertInvalid(
            """
            type Object @example @example {
                field: Int
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Object' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Object",
                "member": "Object",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_NonRepeatableDirectiveAppliedThreeTimes_Fails()
    {
        AssertInvalid(
            """
            type Object @example @example @example {
                field: Int
            }

            directive @example on OBJECT
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Object' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Object",
                "member": "Object",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_NonRepeatableDirectiveAppliedByTypeExtension_Fails()
    {
        AssertInvalid(
            """
            type Object @example {
                field: Int
            }

            extend type Object @example

            directive @example on OBJECT
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Object' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Object",
                "member": "Object",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_NonRepeatableSpecDirectiveAppliedTwice_Fails()
    {
        AssertInvalid(
            """
            type Object {
                field: Int @deprecated(reason: "first") @deprecated(reason: "second")
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@deprecated' on 'Object.field' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Object.field",
                "member": "field",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_NonRepeatableDirectiveAppliedTwiceOnEachTraversedLocation_Fails()
    {
        AssertInvalid(
            """
            schema @example @example {
                query: Query
            }

            type Query @example @example {
                field(argument: Int @example @example): Int @example @example
            }

            input Input {
                field: Int @example @example
            }

            enum Enum {
                VALUE @example @example
            }

            directive @other(argument: Int @example @example) @example @example on OBJECT

            directive @example on ARGUMENT_DEFINITION
                | DIRECTIVE_DEFINITION
                | ENUM_VALUE
                | FIELD_DEFINITION
                | INPUT_FIELD_DEFINITION
                | OBJECT
                | SCHEMA
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'schema' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "member": "default",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Query' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Query",
                "member": "Query",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Query.field(argument:)' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Query.field(argument:)",
                "member": "argument",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Query.field' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Query.field",
                "member": "field",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Input.field' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Input.field",
                "member": "field",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on 'Enum.VALUE' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "Enum.VALUE",
                "member": "VALUE",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on '@other' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "@other",
                "member": "@other",
                "extensions": {}
            }
            """,
            """
            {
                "message": "The non-repeatable directive '@example' on '@other(argument:)' is applied more than once.",
                "code": "HCV0031",
                "severity": "Error",
                "coordinate": "@other(argument:)",
                "member": "argument",
                "extensions": {}
            }
            """);
    }
}
