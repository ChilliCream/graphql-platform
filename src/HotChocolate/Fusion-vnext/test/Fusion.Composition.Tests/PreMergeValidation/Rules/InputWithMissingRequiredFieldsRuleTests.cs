using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class InputWithMissingRequiredFieldsRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new InputWithMissingRequiredFieldsRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "INPUT_WITH_MISSING_REQUIRED_FIELDS"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // If all schemas define "BookFilter" with the required field "title", the rule is
            // satisfied.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        title: String!
                        author: String
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        title: String!
                        yearPublished: Int
                    }
                    """
                ]
            },
            // Multiple required input fields.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        title: String!
                        author: String!
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        title: String!
                        author: String!
                        yearPublished: Int
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
            // If "title" is required in one source schema but missing in another, this violates the
            // rule. In this case, "title" is mandatory in "Schema A" but not defined in "Schema B",
            // causing inconsistency in required fields across schemas.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        title: String!
                        author: String
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        author: String
                        yearPublished: Int
                    }
                    """
                ],
                [
                    "The input type 'BookFilter' in schema 'B' must define the required field " +
                    "'title'."
                ]
            },
            // Multiple required input fields.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        title: String!
                        yearPublished: Int
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        author: String!
                        yearPublished: Int
                    }
                    """
                ],
                [
                    "The input type 'BookFilter' in schema 'A' must define the required field " +
                    "'author'.",

                    "The input type 'BookFilter' in schema 'B' must define the required field " +
                    "'title'."
                ]
            }
        };
    }
}
