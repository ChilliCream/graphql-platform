using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class ExternalArgumentDefaultMismatchRuleTests
{
    private static readonly object s_rule = new ExternalArgumentDefaultMismatchRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new PreMergeValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(_log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new PreMergeValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
                    "The default value '\"de\"' of external argument 'Product.name(language:)' "
                    + "in schema 'B' differs from the default value of '\"en\"' in schema 'A'."
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
                    "The default value '(null)' of external argument 'Product.name(language:)' "
                    + "in schema 'B' differs from the default value of '\"en\"' in schema 'A'."
                ]
            },
            // Here, the "name" field on "Product" is defined without a default value for the
            // "language" argument in the non-external source schema, violating the rule.
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
                    "The default value '\"en\"' of external argument 'Product.name(language:)' "
                    + "in schema 'B' differs from the default value of '(null)' in schema 'A'."
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
                    "The default value '\"sa\"' of external argument "
                    + "'Product.name(localization:)' in schema 'B' differs from the default value "
                    + "of '\"sr\"' in schema 'A'."
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
                    "The default value '(null)' of external argument "
                    + "'Product.name(localization:)' in schema 'B' differs from the default value "
                    + "of '\"sr\"' in schema 'A'."
                ]
            }
        };
    }
}
