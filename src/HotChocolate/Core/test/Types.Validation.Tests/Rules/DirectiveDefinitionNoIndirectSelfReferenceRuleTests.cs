using HotChocolate.Rules;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveDefinitionNoIndirectSelfReferenceRuleTests
    : RuleTestBase<DirectiveDefinitionNoIndirectSelfReferenceRule>
{
    [Fact]
    public void Validate_DirectiveWithoutIndirectSelfReference_Succeeds()
    {
        AssertValid(
            """
            directive @a(arg: Int @u) on OBJECT

            directive @u(arg: In) on ARGUMENT_DEFINITION

            directive @b on INPUT_FIELD_DEFINITION

            input In {
                f: Int @b
            }
            """);
    }

    [Fact]
    public void Validate_InputObjectCycleWithoutDirective_Succeeds()
    {
        AssertValid(
            """
            directive @a(arg: Int @u) on OBJECT

            directive @u(arg: A) on ARGUMENT_DEFINITION

            input A {
                b: B
            }

            input B {
                a: A
            }
            """);
    }

    // Direct self-application is reported by DirectiveDefinitionNoSelfReferenceRule.
    [Fact]
    public void Validate_DirectiveDefinitionDirectSelfApplication_Succeeds()
    {
        AssertValid(
            """
            directive @a(arg: Int @a) @a on ARGUMENT_DEFINITION | DIRECTIVE_DEFINITION
            """);
    }

    [Fact]
    public void Validate_UndefinedTypeAndDirective_Succeeds()
    {
        AssertValid(
            """
            directive @a(arg: Int @u) on OBJECT

            directive @u(arg: Unknown) on ARGUMENT_DEFINITION

            directive @b(arg: Int @v) on OBJECT

            directive @v(arg: In) on ARGUMENT_DEFINITION

            input In {
                f: Int @unknown
            }
            """);
    }

    [Fact]
    public void Validate_CycleThroughOwnArgumentTypeOnly_Succeeds()
    {
        AssertValid(
            """
            directive @a(arg: In) on INPUT_FIELD_DEFINITION

            input In {
                f: Int @a
            }
            """);
    }

    [Fact]
    public void Validate_CycleThroughOwnArgumentEnumTypeOnly_Succeeds()
    {
        AssertValid(
            """
            directive @tag(kind: TagKind!) on ENUM_VALUE

            enum TagKind {
                A @tag(kind: A)
                B
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentTypeInputFieldAppliesDirective_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @u(arg: In) on ARGUMENT_DEFINITION

            input In {
                f: Int @a
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveOnDefinitionReachesItselfThroughType_Fails()
    {
        AssertInvalid(
            """
            directive @a @u on INPUT_FIELD_DEFINITION

            directive @u(arg: In) on DIRECTIVE_DEFINITION

            input In {
                f: Int @a
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentTypeInputObjectAppliesDirective_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on INPUT_OBJECT

            directive @u(arg: In) on ARGUMENT_DEFINITION

            input In @a {
                f: Int
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'In'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ListWrappedArgumentTypeInputFieldAppliesDirective_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @u(arg: [In!]!) on ARGUMENT_DEFINITION

            input In {
                f: Int @a
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentTypeEnumValueAppliesDirective_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on ENUM_VALUE

            directive @u(arg: E) on ARGUMENT_DEFINITION

            enum E {
                X @a
                Y
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'E.X'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentTypeEnumAppliesDirective_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on ENUM

            directive @u(arg: E) on ARGUMENT_DEFINITION

            enum E @a {
                X
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'E'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ArgumentTypeScalarAppliesDirective_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on SCALAR

            directive @u(arg: S) on ARGUMENT_DEFINITION

            scalar S @a
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'S'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_DirectivesReferenceEachOtherThroughArguments_Fails()
    {
        AssertInvalid(
            """
            directive @a(x: Int @b) on ARGUMENT_DEFINITION

            directive @b(y: Int @a) on ARGUMENT_DEFINITION
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(x:)', '@b(y:)'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ThreeDirectivesReferenceEachOtherThroughArguments_Fails()
    {
        AssertInvalid(
            """
            directive @a(x: Int @b) on ARGUMENT_DEFINITION

            directive @b(y: Int @c) on ARGUMENT_DEFINITION

            directive @c(z: Int @a) on ARGUMENT_DEFINITION
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(x:)', '@b(y:)', '@c(z:)'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NestedInputObjectChainAppliesDirective_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @u(arg: In1) on ARGUMENT_DEFINITION

            input In1 {
                f: In2
            }

            input In2 {
                g: Int @a
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'In1.f', 'In2.g'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_TwoDirectivesShareReferencingType_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @b(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @u(arg: In) on ARGUMENT_DEFINITION

            input In {
                f: Int @a @b
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@b' references itself via: '@b(arg:)', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@b",
                "member": "@b",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_RepeatableDirectiveAppliedTwice_Fails()
    {
        AssertInvalid(
            """
            directive @a(arg: Int @u) repeatable on INPUT_FIELD_DEFINITION

            directive @u(arg: In) on ARGUMENT_DEFINITION

            input In {
                f: Int @a @a
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_DefinitionReachesCycleItIsNotPartOf_Fails()
    {
        AssertInvalid(
            """
            directive @outer(arg: Int @u) on OBJECT

            directive @inner(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @u(arg: In) on ARGUMENT_DEFINITION

            input In {
                f: Int @inner
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@inner' references itself via: '@inner(arg:)', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@inner",
                "member": "@inner",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_CycleThroughDirectiveAndTypeReachedFromOtherDefinition_Fails()
    {
        AssertInvalid(
            """
            directive @r(arg: A) on OBJECT

            directive @d(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @u(arg: B) on ARGUMENT_DEFINITION

            input A {
                b: B
                n: Int @d
            }

            input B {
                a: A
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@d' references itself via: '@d(arg:)', '@u(arg:)', 'B.a', 'A.n'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@d",
                "member": "@d",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_CycleThroughDirectiveAndTypeReachedFromOtherDefinitionFieldOrderSwapped_Fails()
    {
        AssertInvalid(
            """
            directive @r(arg: A) on OBJECT

            directive @d(arg: Int @u) on INPUT_FIELD_DEFINITION

            directive @u(arg: B) on ARGUMENT_DEFINITION

            input A {
                n: Int @d
                b: B
            }

            input B {
                a: A
            }
            """,
            """
            {
                "message": "Invalid circular reference. The directive definition '@d' references itself via: '@d(arg:)', '@u(arg:)', 'B.a', 'A.n'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@d",
                "member": "@d",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_CycleThroughArgumentAddedInCode_Fails()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            directive @a on INPUT_FIELD_DEFINITION

            directive @u(arg: In) on ARGUMENT_DEFINITION

            input In {
                f: Int @a
            }
            """);
        var argument = new MutableInputFieldDefinition(
            "arg",
            schema.Types.GetType<MutableInputObjectTypeDefinition>("In"));
        argument.Directives.Add(new Mutable.Directive(schema.DirectiveDefinitions["u"]));
        schema.DirectiveDefinitions["a"].Arguments.Add(argument);

        // act & assert
        AssertInvalid(
            schema,
            """
            {
                "message": "Invalid circular reference. The directive definition '@a' references itself via: '@a(arg:)', '@u(arg:)', 'In.f'.",
                "code": "HCV0033",
                "severity": "Error",
                "coordinate": "@a",
                "member": "@a",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }
}
