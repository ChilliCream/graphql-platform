using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

public sealed class ExternalMissingOnBaseRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new ExternalMissingOnBaseRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "EXTERNAL_MISSING_ON_BASE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the "name" field on "Product" is defined in source schema A and marked as
            // @external in source schema B, which is valid because there is a base definition in
            // source schema A.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String
                    }
                    """,
                    """
                    # Source schema B
                    type Product {
                        id: ID
                        name: String @external
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
            // In this example, the "name" field on "Product" is marked as @external in source
            // schema B but has no non-@external declaration in any other source schema, violating
            // the rule.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                    }
                    """,
                    """
                    # Source schema B
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """
                ],
                [
                    "The external field 'Product.name' in schema 'B' is not defined " +
                    "(non-external) in any other schema."
                ]
            },
            // The "name" field is marked as @external in both source schemas.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """,
                    """
                    # Source schema B
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """
                ],
                [
                    "The external field 'Product.name' in schema 'A' is not defined " +
                    "(non-external) in any other schema.",

                    "The external field 'Product.name' in schema 'B' is not defined " +
                    "(non-external) in any other schema."
                ]
            }
        };
    }
}
