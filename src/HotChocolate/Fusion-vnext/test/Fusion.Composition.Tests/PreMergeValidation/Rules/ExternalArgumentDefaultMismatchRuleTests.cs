using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ExternalArgumentDefaultMismatchRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new ExternalArgumentDefaultMismatchRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the "name" field on "Product" is defined in one source schema and marked as
            // @external in another. The argument "language" has the same default value in both
            // source schemas, satisfying the rule.
            {
                [
                    """
                    type Product {
                        name(language: String = "en"): String
                    }
                    """,
                    """
                    type Product {
                        name(language: String = "en"): String @external
                    }
                    """
                ]
            },
            // Here, the "name" field on "Product" is defined with multiple arguments. Both
            // arguments have the same default value in the source schemas, satisfying the rule.
            {
                [
                    """
                    type Product {
                        name(language: String = "en", localization: String = "sr"): String
                    }
                    """,
                    """
                    type Product {
                        name(localization: String = "sr", language: String = "en"): String @external
                    }
                    """,
                    """
                    type Product {
                        name(language: String = "en", localization: String = "sr"): String @external
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
                    type Product {
                        name(language: String = "en"): String
                    }
                    """,
                    """
                    type Product {
                        name(language: String = "de"): String @external
                    }
                    """
                ],
                [
                    "The argument with schema coordinate 'Product.name(language:)' has " +
                    "inconsistent default values."
                ]
            },
            // In the following example, the "name" field on "Product" is defined in one source
            // schema and marked as @external in another. The argument "language" has a default
            // value in the source schema where the field is defined, but it does not have a default
            // value in the source schema where the field is marked as @external, violating the
            // rule.
            {
                [
                    """
                    type Product {
                        name(language: String = "en"): String
                    }
                    """,
                    """
                    type Product {
                        name(language: String): String @external
                    }
                    """
                ],
                [
                    "The argument with schema coordinate 'Product.name(language:)' has " +
                    "inconsistent default values."
                ]
            },
            // Here, the "name" field on "Product" is defined without a default value in the
            // non-external source schema, violating the rule.
            {
                [
                    """
                    type Product {
                        name(language: String): String
                    }
                    """,
                    """
                    type Product {
                        name(language: String = "en"): String @external
                    }
                    """
                ],
                [
                    "The argument with schema coordinate 'Product.name(language:)' has " +
                    "inconsistent default values."
                ]
            },
            // Here, the "name" field on "Product" is defined with multiple arguments. One argument
            // has a matching default value, whilst the other does not, violating the rule.
            {
                [
                    """
                    type Product {
                        name(language: String = "en", localization: String = "sr"): String
                    }
                    """,
                    """
                    type Product {
                        name(language: String = "en", localization: String = "sa"): String @external
                    }
                    """
                ],
                [
                    "The argument with schema coordinate 'Product.name(localization:)' has " +
                    "inconsistent default values."
                ]
            },
            // Here, the "name" field on "Product" is defined with multiple arguments. One argument
            // has a matching default value, whilst the other omits the value completely, violating
            // the rule.
            {
                [
                    """
                    type Product {
                        name(language: String = "en", localization: String = "sr"): String
                    }
                    """,
                    """
                    type Product {
                        name(language: String = "en", localization: String): String @external
                    }
                    """
                ],
                [
                    "The argument with schema coordinate 'Product.name(localization:)' has " +
                    "inconsistent default values."
                ]
            }
        };
    }
}
