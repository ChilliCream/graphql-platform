using HotChocolate.Configuration.Validation;

namespace HotChocolate.Types.Validation;

public class DirectiveValidationRuleTests : TypeValidationTestBase
{
    [Fact]
    public void RejectRequiredArgumentThatIsDeprecated()
    {
        ExpectError(@"
          type Query { stub: String }

          directive @badDirective(
            badArg: String! @deprecated
          ) on FIELD
        ");
    }

    [Fact]
    public void AcceptNonRequiredArgumentThatIsDeprecated()
    {
        ExpectValid(@"
          type Query { stub: String }

          directive @badDirective(
            optionalArg: String @deprecated
          ) on FIELD
        ");
    }

    [Fact]
    public void RejectArgumentsWithInvalidName()
    {
        ExpectError(@"
          type Query { stub: String }

          directive @badDirective(
            __badArg: String
          ) on FIELD
        ");
    }

    [Fact]
    public void RejectDirectiveWithInvalidName()
    {
        ExpectError(@"
          type Query { stub: String }

          directive @__badDirective() on FIELD
        ");
    }

    [Fact]
    public void RejectDirectiveArgumentWithIncompatibleDefaultValue()
    {
        ExpectError("""
          type Query { stub: String }

          directive @foo(arg: Int = "abc") on FIELD
        """);
    }

    [Fact]
    public void RejectDirectiveReachingItselfThroughDirectiveExtension()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a on DIRECTIVE_DEFINITION | INPUT_FIELD_DEFINITION

            input I {
              f: Int @a
            }

            directive @b(arg: I) on DIRECTIVE_DEFINITION

            extend directive @a @b
            """);
    }

    [Fact]
    public void RejectDirectiveReachingItselfThroughArgumentDirective()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a(arg: Int @b) on INPUT_FIELD_DEFINITION

            directive @b(arg: I) on ARGUMENT_DEFINITION

            input I {
              f: Int @a
            }
            """);
    }

    [Fact]
    public void RejectDirectiveReachingItselfThroughEnumValue()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a @b on ENUM_VALUE

            directive @b(arg: E) on DIRECTIVE_DEFINITION

            enum E {
              A @a
              B
            }
            """);
    }

    [Fact]
    public void RejectDirectiveReachingItselfThroughListOfAnnotatedType()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a @b on ENUM

            directive @b(arg: [E!]) on DIRECTIVE_DEFINITION

            enum E @a {
              A
            }
            """);
    }

    [Fact]
    public void RejectDirectiveReachingItselfThroughNestedInputType()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a(arg: Int @b) on INPUT_FIELD_DEFINITION

            directive @b(arg: I1) on ARGUMENT_DEFINITION

            input I1 {
              f: I2
            }

            input I2 {
              g: Int @a
            }
            """);
    }

    [Fact]
    public void RejectDirectiveReachingItselfThroughInputObjectType()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a @b on INPUT_OBJECT

            directive @b(arg: I) on DIRECTIVE_DEFINITION

            input I @a {
              f: Int
            }
            """);
    }

    [Fact]
    public void RejectDirectivesReachingEachOtherThroughArgumentDirectives()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a(x: Int @b) on ARGUMENT_DEFINITION

            directive @b(y: Int @a) on ARGUMENT_DEFINITION
            """);
    }

    [Fact]
    public void RejectCycleThroughSeveralDirectivesOnce()
    {
        ExpectError(
            """
            type Query { stub: String }

            directive @a @b on INPUT_FIELD_DEFINITION

            directive @b @c on DIRECTIVE_DEFINITION

            directive @c(arg: I) on DIRECTIVE_DEFINITION

            input I {
              f: Int @a
            }
            """);
    }

    [Fact]
    public void AcceptDirectiveReachingItselfOnlyThroughOwnArgumentType()
    {
        ExpectValid(
            """
            type Query { stub: String }

            directive @a(arg: I) on INPUT_FIELD_DEFINITION

            input I {
              f: Int @a
            }
            """);
    }

    [Fact]
    public void AcceptDirectiveReachingItselfWhenStrictValidationIsDisabled()
    {
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                """
                type Query { stub: String }

                directive @a on DIRECTIVE_DEFINITION | INPUT_FIELD_DEFINITION

                input I {
                  f: Int @a
                }

                directive @b(arg: I) on DIRECTIVE_DEFINITION

                extend directive @a @b
                """)
            .Use(_ => _ => default)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        Assert.True(schema.DirectiveTypes.ContainsName("a"));
    }
}
