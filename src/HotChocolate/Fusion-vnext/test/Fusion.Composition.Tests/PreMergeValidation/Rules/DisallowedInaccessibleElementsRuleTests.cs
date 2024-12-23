using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class DisallowedInaccessibleElementsRuleTests
{
    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new DisallowedInaccessibleElementsRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new DisallowedInaccessibleElementsRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(log);
        Assert.Equal("DISALLOWED_INACCESSIBLE", log.First().Code);
        Assert.Equal(LogSeverity.Error, log.First().Severity);
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the String type is not marked as @inaccessible, which adheres to the rule.
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

    public static TheoryData<string[]> InvalidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the String scalar is marked as @inaccessible. This violates the rule
            // because String is a required built-in type that cannot be inaccessible.
            {
                [
                    """
                    scalar String @inaccessible

                    type Product {
                        price: Float
                        name: String
                    }
                    """
                ]
            },
            // In this example, the introspection type __Type is marked as @inaccessible. This
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
                ]
            },
            // Inaccessible built-in directive argument.
            {
                [
                    """
                    directive @skip(if: Boolean! @inaccessible)
                        on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
                    """
                ]
            }
        };
    }
}
