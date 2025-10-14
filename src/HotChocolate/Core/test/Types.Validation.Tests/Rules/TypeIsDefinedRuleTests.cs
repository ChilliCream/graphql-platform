using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class TypeIsDefinedRuleTests : RuleTestBase<TypeIsDefinedRule>
{
    [Fact]
    public void Validate_TypeIsDefined_Succeeds()
    {
        AssertValid(
            """
            type FooObject {
                fooNullable(arg: URI): URI
                fooNonNullable(arg: URI!): URI!
                fooList(arg: [URI!]!): [URI!]!
            }

            input FooInput {
                fooNullable: URI
                fooNonNullable: URI!
                fooList: [URI!]!
            }

            scalar URI
            """);
    }

    [Fact]
    public void Validate_TypeIsSpecScalar_Succeeds()
    {
        AssertValid(
            """
            type FooObject {
                fooNullable(arg: Int): Int
                fooNonNullable(arg: Int!): Int!
                fooList(arg: [Int!]!): [Int!]!
            }

            input FooInput {
                fooNullable: Int
                fooNonNullable: Int!
                fooList: [Int!]!
            }
            """);
    }

    [Fact]
    public void Validate_OutputFieldTypeIsUndefined_Fails()
    {
        AssertInvalid(
            """
            type FooObject {
                foo: URI
            }
            """,
            """
            {
                "message": "The type 'URI' of field 'FooObject.foo' is not defined in the schema.",
                "code": "HCV0021",
                "severity": "Error",
                "coordinate": "FooObject.foo",
                "member": "foo",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_InputFieldTypeIsUndefined_Fails()
    {
        AssertInvalid(
            """
            input FooInput {
                foo: URI
            }
            """,
            """
            {
                "message": "The type 'URI' of field 'FooInput.foo' is not defined in the schema.",
                "code": "HCV0021",
                "severity": "Error",
                "coordinate": "FooInput.foo",
                "member": "foo",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentTypeIsUndefined_Fails()
    {
        AssertInvalid(
            """
            type FooObject {
                foo(arg: URI): Int
            }
            """,
            """
            {
                "message": "The type 'URI' of argument 'FooObject.foo(arg:)' is not defined in the schema.",
                "code": "HCV0022",
                "severity": "Error",
                "coordinate": "FooObject.foo(arg:)",
                "member": "arg",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveArgumentTypeIsUndefined_Fails()
    {
        AssertInvalid(
            "directive @foo(arg: URI) on FIELD_DEFINITION",
            """
            {
                "message": "The type 'URI' of argument '@foo(arg:)' is not defined in the schema.",
                "code": "HCV0022",
                "severity": "Error",
                "coordinate": "@foo(arg:)",
                "member": "arg",
                "extensions": {}
            }
            """);
    }
}
