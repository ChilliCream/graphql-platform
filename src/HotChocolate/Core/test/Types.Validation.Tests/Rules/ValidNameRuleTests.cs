using HotChocolate.Rules;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Types.Validation.Rules;

public sealed class ValidNameRuleTests : RuleTestBase<ValidNameRule>
{
    [Fact]
    public void Validate_ValidTypeNames_Succeeds()
    {
        AssertValid(
            """
            enum FooEnum
            input FooInput
            interface FooInterface
            scalar FooScalar
            type FooObject
            union FooUnion
            # Introspection types
            enum __DirectiveLocation
            enum __TypeKind
            type __Directive
            type __EnumValue
            type __Field
            type __InputValue
            type __Schema
            type __Type
            """,
            s =>
            {
                ((MutableEnumTypeDefinition)s.Types["__DirectiveLocation"]).IsIntrospectionType = true;
                ((MutableEnumTypeDefinition)s.Types["__TypeKind"]).IsIntrospectionType = true;
                ((MutableObjectTypeDefinition)s.Types["__Directive"]).IsIntrospectionType = true;
                ((MutableObjectTypeDefinition)s.Types["__EnumValue"]).IsIntrospectionType = true;
                ((MutableObjectTypeDefinition)s.Types["__Field"]).IsIntrospectionType = true;
                ((MutableObjectTypeDefinition)s.Types["__InputValue"]).IsIntrospectionType = true;
                ((MutableObjectTypeDefinition)s.Types["__Schema"]).IsIntrospectionType = true;
                ((MutableObjectTypeDefinition)s.Types["__Type"]).IsIntrospectionType = true;
            });
    }

    [Fact]
    public void Validate_ValidFieldNames_Succeeds()
    {
        AssertValid(
            """
            input FooInput { id: ID! }
            interface FooInterface { id: ID! }
            type FooObject { id: ID! }
            # Introspection fields
            type Query {
                __schema: Any
                __type: Any
            }
            """,
            s =>
            {
                s.QueryType?.Fields["__schema"].IsIntrospectionField = true;
                s.QueryType?.Fields["__type"].IsIntrospectionField = true;
            });
    }

    [Fact]
    public void Validate_ValidArgumentNames_Succeeds()
    {
        AssertValid(
            """
            directive @foo(arg: Int) on FIELD_DEFINITION

            type Query {
                foo(arg: Int): Foo
            }
            """);
    }

    [Fact]
    public void Validate_ValidDirectiveName_Succeeds()
    {
        AssertValid("directive @foo on FIELD_DEFINITION");
    }

    [Fact]
    public void Validate_ValidEnumValueName_Succeeds()
    {
        AssertValid("enum Foo { VALUE }");
    }

    [Fact]
    public void Validate_InvalidEnumName_Fails()
    {
        AssertInvalid(
            "enum __FooEnum",
            """
            {
                "message": "The name of the type system member '__FooEnum' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "__FooEnum",
                "member": "__FooEnum",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidInputName_Fails()
    {
        AssertInvalid(
            "input __FooInput",
            """
            {
                "message": "The name of the type system member '__FooInput' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "__FooInput",
                "member": "__FooInput",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidInterfaceName_Fails()
    {
        AssertInvalid(
            "interface __FooInterface",
            """
            {
                "message": "The name of the type system member '__FooInterface' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "__FooInterface",
                "member": "__FooInterface",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidScalarName_Fails()
    {
        AssertInvalid(
            "scalar __FooScalar",
            """
            {
                "message": "The name of the type system member '__FooScalar' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "__FooScalar",
                "member": "__FooScalar",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidObjectName_Fails()
    {
        AssertInvalid(
            "type __FooObject",
            """
            {
                "message": "The name of the type system member '__FooObject' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "__FooObject",
                "member": "__FooObject",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidUnionName_Fails()
    {
        AssertInvalid(
            "union __FooUnion",
            """
            {
                "message": "The name of the type system member '__FooUnion' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "__FooUnion",
                "member": "__FooUnion",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidInputFieldName_Fails()
    {
        AssertInvalid(
            """
            input FooInput {
                __id: ID!
            }
            """,
            """
            {
                "message": "The name of the type system member 'FooInput.__id' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "FooInput.__id",
                "member": "__id",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidInterfaceFieldName_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface {
                __id: ID!
            }
            """,
            """
            {
                "message": "The name of the type system member 'FooInterface.__id' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "FooInterface.__id",
                "member": "__id",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidObjectFieldName_Fails()
    {
        AssertInvalid(
            """
            type FooObject {
                __id: ID!
            }
            """,
            """
            {
                "message": "The name of the type system member 'FooObject.__id' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "FooObject.__id",
                "member": "__id",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidDirectiveArgumentName_Fails()
    {
        AssertInvalid(
            "directive @foo(__arg: Int) on FIELD_DEFINITION",
            """
            {
                "message": "The name of the type system member '@foo(__arg:)' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "@foo(__arg:)",
                "member": "__arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidObjectFieldArgumentName_Fails()
    {
        AssertInvalid(
            """
            type Query {
                foo(__arg: Int): Foo
            }
            """,
            """
            {
                "message": "The name of the type system member 'Query.foo(__arg:)' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "Query.foo(__arg:)",
                "member": "__arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidInterfaceFieldArgumentName_Fails()
    {
        AssertInvalid(
            """
            interface FooInterface {
                foo(__arg: Int): Foo
            }
            """,
            """
            {
                "message": "The name of the type system member 'FooInterface.foo(__arg:)' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "FooInterface.foo(__arg:)",
                "member": "__arg",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidDirectiveName_Fails()
    {
        AssertInvalid(
            "directive @__foo on FIELD_DEFINITION",
            """
            {
                "message": "The name of the type system member '@__foo' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "@__foo",
                "member": "@__foo",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidEnumValueName_Fails()
    {
        AssertInvalid(
            """
            enum Foo {
                __VALUE
            }
            """,
            """
            {
                "message": "The name of the type system member 'Foo.__VALUE' must not start with two underscores '__'.",
                "code": "HCV0002",
                "severity": "Error",
                "coordinate": "Foo.__VALUE",
                "member": "__VALUE",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Names.Reserved-Names"
                }
            }
            """);
    }
}
