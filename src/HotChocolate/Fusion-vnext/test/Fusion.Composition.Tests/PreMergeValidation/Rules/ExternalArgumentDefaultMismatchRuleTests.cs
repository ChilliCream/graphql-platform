using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ExternalArgumentDefaultMismatchRuleTests
{
    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new ExternalArgumentDefaultMismatchRule()]);

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
        var preMergeValidator = new PreMergeValidator([new ExternalArgumentDefaultMismatchRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(log);
        Assert.Equal("EXTERNAL_ARGUMENT_DEFAULT_MISMATCH", log.First().Code);
        Assert.Equal(LogSeverity.Error, log.First().Severity);
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the `name` field on Product is defined in one source schema and marked as
            // @external in another. The argument `language` has the same default value in both
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
            // Here, the `name` field on Product is defined with multiple arguments. Both arguments
            // have the same default value in the source schemas, satisfying the rule.
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

    public static TheoryData<string[]> InvalidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the `name` field on Product is defined in one source schema and marked as
            // @external in another. The argument `language` has different default values in the
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
                ]
            },
            // Here, the `name` field on Product is defined without a default value in one source
            // schema, violating the rule.
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
                ]
            },
            // Here, the `name` field on Product is defined without a default value in the
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
                ]
            },
            // Here, the `name` field on Product is defined with multiple arguments. One argument
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
                ]
            },
            // Here, the `name` field on Product is defined with multiple arguments. One argument
            // has a matching default value, whilst the other omits the value completely,
            // violating the rule.
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
                ]
            }
        };
    }
}
