using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class EnumValuesMustBeTheSameAcrossSchemasRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new EnumValuesMustBeTheSameAcrossSchemasRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _preMergeValidator.Validate(context);

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
        var result = _preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(
            context.Log.All(e => e.Code == "ENUM_VALUES_MUST_BE_THE_SAME_ACROSS_SCHEMAS"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, both source schemas define "Genre" with the same value "FANTASY",
            // satisfying the rule.
            {
                [
                    """
                    enum Genre {
                        FANTASY
                    }
                    """,
                    """
                    enum Genre {
                        FANTASY
                    }
                    """
                ]
            },
            // Here, the two definitions of "Genre" have shared values and additional values
            // declared as @inaccessible, satisfying the rule.
            {
                [
                    """
                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION @inaccessible
                    }
                    """,
                    """
                    enum Genre {
                        FANTASY
                    }
                    """
                ]
            },
            // Here, the two definitions of "Genre" have shared values in a differing order
            {
                [
                    """
                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION @inaccessible
                        ANIMATED
                    }
                    """,
                    """
                    enum Genre {
                        ANIMATED
                        FANTASY
                        CRIME @inaccessible
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
            // Here, the "name" field on "Product" is defined in one source schema and marked as
            // @external in another. The argument "language" has different default values in the
            // two source schemas, violating the rule.
            {
                [
                    """
                    enum Genre {
                        FANTASY
                    }
                    """,
                    """
                    enum Genre {
                        SCIENCE_FICTION
                    }
                    """
                ],
                [
                    "The enum 'Genre' in schema 'A' must contain consistent values across schemas.",
                    "The enum 'Genre' in schema 'B' must contain consistent values across schemas."
                ]
            }
        };
    }
}
