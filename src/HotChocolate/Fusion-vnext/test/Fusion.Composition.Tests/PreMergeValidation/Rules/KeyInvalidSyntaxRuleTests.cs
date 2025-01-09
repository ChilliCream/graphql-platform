using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class KeyInvalidSyntaxRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator = new([new KeyInvalidSyntaxRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "KEY_INVALID_SYNTAX"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "fields" argument is a correctly formed selection set:
            // "sku featuredItem { id }" is properly balanced and contains no syntax errors.
            {
                [
                    """
                    type Product @key(fields: "sku featuredItem { id }") {
                        sku: String!
                        featuredItem: Node!
                    }

                    interface Node {
                        id: ID!
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
            // Here, the selection set "featuredItem { id" is missing the closing brace "}". It is
            // thus invalid syntax, causing a KEY_INVALID_SYNTAX error.
            {
                [
                    """
                    type Product @key(fields: "featuredItem { id") {
                        featuredItem: Node!
                        sku: String!
                    }

                    interface Node {
                        id: ID!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' contains invalid syntax in " +
                    "the 'fields' argument."
                ]
            }
        };
    }
}
