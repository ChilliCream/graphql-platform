using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputFieldDefaultMismatchRuleTests
{
    private static readonly object s_rule = new InputFieldDefaultMismatchRule();
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
        Assert.True(_log.All(e => e.Code == "INPUT_FIELD_DEFAULT_MISMATCH"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
                    # Schema A
                    input BookFilter {
                        genre: Genre = FANTASY
                    }

                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        genre: Genre = FANTASY
                    }

                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION
                    }
                    """
                ]
            },
            // If only one of the source schemas defines a default value for a given input field,
            // the composition is still valid.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        genre: Genre
                    }

                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        genre: Genre = FANTASY
                    }

                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION
                    }
                    """
                ]
            },
            // Multiple input fields.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        genre1: Genre = FANTASY
                        genre2: Genre
                    }

                    enum Genre {
                        FANTASY
                        SCIENCE_FICTION
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        genre1: Genre
                        genre2: Genre = SCIENCE_FICTION
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
                    # Schema A
                    input BookFilter {
                        minPageCount: Int = 10
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        minPageCount: Int = 20
                    }
                    """
                ],
                [
                    "The default value '10' of input field 'BookFilter.minPageCount' in schema "
                    + "'A' differs from the default value of '20' in schema 'B'."
                ]
            },
            // Two different default values, and one without a default value.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        minPageCount: Int = 10
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        minPageCount: Int
                    }
                    """,
                    """
                    # Schema C
                    input BookFilter {
                        minPageCount: Int = 20
                    }
                    """
                ],
                [
                    "The default value '10' of input field 'BookFilter.minPageCount' in schema "
                    + "'A' differs from the default value of '20' in schema 'C'."
                ]
            },
            // Three different default values.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        minPageCount: Int = 10
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        minPageCount: Int = 20
                    }
                    """,
                    """
                    # Schema C
                    input BookFilter {
                        minPageCount: Int = 30
                    }
                    """
                ],
                [
                    "The default value '10' of input field 'BookFilter.minPageCount' in schema "
                    + "'A' differs from the default value of '20' in schema 'B'.",

                    "The default value '20' of input field 'BookFilter.minPageCount' in schema "
                    + "'B' differs from the default value of '30' in schema 'C'."
                ]
            }
        };
    }
}
