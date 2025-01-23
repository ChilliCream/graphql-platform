using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class DisallowedInaccessibleElementsRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new DisallowedInaccessibleElementsRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(context.Log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "DISALLOWED_INACCESSIBLE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the "String" type is not marked as @inaccessible, which adheres to the rule.
            {
                [
                    """
                    type Product {
                        price: Float
                        name: String
                    }
                    """
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // In this example, the "String" scalar is marked as @inaccessible. This violates the
            // rule because "String" is a required built-in type that cannot be inaccessible.
            {
                [
                    """
                    scalar String @inaccessible

                    type Product {
                        price: Float
                        name: String
                    }
                    """
                ],
                [
                    "The built-in scalar type 'String' in schema 'A' is not accessible."
                ]
            },
            // In this example, the introspection type "__Type" is marked as @inaccessible. This
            // violates the rule because introspection types must remain accessible for GraphQL
            // introspection queries to work.
            {
                [
                    """
                    type __Type @inaccessible {
                        kind: __TypeKind!
                        name: String
                        fields(includeDeprecated: Boolean = false): [__Field!]
                    }
                    """
                ],
                [
                    "The introspection type '__Type' in schema 'A' is not accessible."
                ]
            },
            // Inaccessible introspection field.
            {
                [
                    """
                    type __Type {
                        kind: __TypeKind! @inaccessible
                        name: String
                        fields(includeDeprecated: Boolean = false): [__Field!]
                    }
                    """
                ],
                [
                    "The introspection field '__Type.kind' in schema 'A' is not accessible."
                ]
            },
            // Inaccessible introspection argument.
            {
                [
                    """
                    type __Type {
                        kind: __TypeKind!
                        name: String
                        fields(includeDeprecated: Boolean = false @inaccessible): [__Field!]
                    }
                    """
                ],
                [
                    "The introspection argument '__Type.fields(includeDeprecated:)' in schema " +
                    "'A' is not accessible."
                ]
            },
            // Inaccessible built-in directive argument.
            {
                [
                    """
                    directive @skip(if: Boolean! @inaccessible)
                        on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
                    """
                ],
                [
                    "The built-in directive argument '@skip(if:)' in schema 'A' is not accessible."
                ]
            }
        };
    }
}
