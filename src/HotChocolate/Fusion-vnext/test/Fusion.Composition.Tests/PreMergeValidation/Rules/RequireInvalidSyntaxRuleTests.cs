using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class RequireInvalidSyntaxRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator = new([new RequireInvalidSyntaxRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "REQUIRE_INVALID_SYNTAX"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @require directive’s "fields" argument is a valid
            // selection map and satisfies the rule.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        profile(name: String! @require(fields: "name")): Profile
                    }

                    type Profile {
                        id: ID!
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
            // In the following example, the @require directive’s "fields" argument has invalid
            // syntax because it is missing a closing brace.
            {
                [
                    """
                    type Book {
                        id: ID!
                        title(lang: String! @require(fields: "author { name ")): String
                    }

                    type Author {
                        name: String
                    }
                    """
                ],
                [
                    "The @require directive on argument 'Book.title(lang:)' in schema 'A' " +
                    "contains invalid syntax in the 'fields' argument."
                ]
            }
        };
    }
}
