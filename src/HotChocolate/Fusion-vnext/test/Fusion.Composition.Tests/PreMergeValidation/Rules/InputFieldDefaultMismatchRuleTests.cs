using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class InputFieldDefaultMismatchRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new InputFieldDefaultMismatchRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "INPUT_FIELD_DEFAULT_MISMATCH"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example both source schemas have an input field "genre" with
            // the same default value. This is valid.
            {
                [
                    """
                    # Source schema A
                    input BookFilter {
                        genre: Genre = FANTASY
                    }

                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION
                    }
                    """,
                    """
                    # Source schema B
                    input BookFilter {
                        genre: Genre = FANTASY
                    }

                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION
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
            // In the following example both source schemas define an input field "minPageCount"
            // with different default values. This is invalid.
            {
                [
                    """
                    # Source schema A
                    input BookFilter {
                        minPageCount: Int = 10
                    }
                    """,
                    """
                    # Source schema B
                    input BookFilter {
                        minPageCount: Int = 20
                    }
                    """
                ],
                [
                    "The field 'minPageCount' on type 'BookFilter' has inconsistent default values."
                ]
            }
        };
    }
}
