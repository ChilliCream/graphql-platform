using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class ValidDefaultValueRuleTests : RuleTestBase<ValidDefaultValueRule>
{
    [Fact]
    public void Validate_CompatibleScalarDefault_Succeeds()
    {
        AssertValid(
            """
            type Query {
                field(arg: Int = 123): Int
            }
            """);
    }

    [Fact]
    public void Validate_IncompatibleScalarDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: Int = "abc"): Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is not compatible with the type 'Int'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NullForNonNullDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: Int! = null): Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is not compatible with the type 'Int!'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_UnknownInputObjectFieldInDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: FooInput = { missing: 1 }): Int
            }

            input FooInput {
                a: Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' specifies the unknown input field 'missing'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NestedListElementMismatch_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: [Int] = [1, "x"]): Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is not compatible with the type 'Int' at path '[1]'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "path": "[1]",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NestedInputFieldListElementMismatch_Fails()
    {
        AssertInvalid(
            """
            type Query { stub: Int }

            input FooInput {
                a: [Int] = [1, "x"]
            }
            """,
            """
            {
                "message": "The default value of input field 'FooInput.a' is not compatible with the type 'Int' at path '[1]'.",
                "code": "HCV0029",
                "severity": "Error",
                "coordinate": "FooInput.a",
                "member": "a",
                "extensions": {
                    "path": "[1]",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NestedUnknownInputFieldInDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: BarInput = { inner: { missing: 1 } }): Int
            }

            input BarInput {
                inner: FooInput
            }

            input FooInput {
                a: Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' specifies the unknown input field 'missing' at path 'inner'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "path": "inner",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NestedMissingRequiredInputFieldInDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: BarInput = { inner: {} }): Int
            }

            input BarInput {
                inner: FooInput
            }

            input FooInput {
                a: Int
                b: Int!
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is missing the required input field 'b' at path 'inner'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "path": "inner",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NestedOneOfDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: BarInput = { inner: {} }): Int
            }

            input BarInput {
                inner: FooOneOf
            }

            input FooOneOf @oneOf {
                a: Int
                b: Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' must specify exactly one non-null field for the oneOf input object 'FooOneOf' at path 'inner'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "path": "inner",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_CompatibleInputFieldDefault_Succeeds()
    {
        AssertValid(
            """
            type Query { stub: Int }

            input FooInput {
                a: Int = 1
            }
            """);
    }

    [Fact]
    public void Validate_IncompatibleInputFieldDefault_Fails()
    {
        AssertInvalid(
            """
            type Query { stub: Int }

            input FooInput {
                a: Int = "x"
            }
            """,
            """
            {
                "message": "The default value of input field 'FooInput.a' is not compatible with the type 'Int'.",
                "code": "HCV0029",
                "severity": "Error",
                "coordinate": "FooInput.a",
                "member": "a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NonNullScalarIncompatibleDefault_ReportsNonNullTypeName()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: Int! = "abc"): Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is not compatible with the type 'Int!'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_OneOfDefaultWithTwoFields_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: FooInput = { a: 1, b: 2 }): Int
            }

            input FooInput @oneOf {
                a: Int
                b: Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' must specify exactly one non-null field for the oneOf input object 'FooInput'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_OneOfDefaultWithZeroFields_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: FooInput = {}): Int
            }

            input FooInput @oneOf {
                a: Int
                b: Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' must specify exactly one non-null field for the oneOf input object 'FooInput'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_MissingRequiredInputFieldInDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: FooInput = {}): Int
            }

            input FooInput {
                a: Int
                b: Int!
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is missing the required input field 'b'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_EnumDefaultWrongKind_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: FooEnum = 5): Int
            }

            enum FooEnum {
                VALUE
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is not compatible with the type 'FooEnum'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveArgumentIncompatibleDefault_Fails()
    {
        AssertInvalid(
            """
            directive @foo(arg: Int = "x") on FIELD
            """,
            """
            {
                "message": "The default value of argument '@foo(arg:)' is not compatible with the type 'Int'.",
                "code": "HCV0028",
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
    public void Validate_InterfaceArgumentIncompatibleDefault_Fails()
    {
        AssertInvalid(
            """
            type Query { stub: Int }

            interface Foo {
                field(arg: Int = "x"): Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Foo.field(arg:)' is not compatible with the type 'Int'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Foo.field(arg:)",
                "member": "arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ListSingletonCoercion_Succeeds()
    {
        AssertValid(
            """
            type Query {
                field(arg: [Int] = 1): Int
            }
            """);
    }

    [Fact]
    public void Validate_NullInNonNullListElementDefault_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: [Int!] = [null]): Int
            }
            """,
            """
            {
                "message": "The default value of argument 'Query.field(arg:)' is not compatible with the type 'Int!' at path '[0]'.",
                "code": "HCV0028",
                "severity": "Error",
                "coordinate": "Query.field(arg:)",
                "member": "arg",
                "extensions": {
                    "path": "[0]",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NullInNullableListElementDefault_Succeeds()
    {
        AssertValid(
            """
            type Query {
                field(arg: [Int] = [null]): Int
            }
            """);
    }
}
