using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class EnumValuesMismatchRuleTests
{
    private static readonly object s_rule = new EnumValuesMismatchRule();
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
        Assert.True(_log.All(e => e.Code == "ENUM_VALUES_MISMATCH"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
            // Here, the two definitions of "Genre" have shared values in a differing order.
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
            // Here, the two definitions of "Genre" have different values ("FANTASY" and
            // "SCIENCE_FICTION"), violating the rule.
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
                    "The enum type 'Genre' in schema 'A' must define the value 'SCIENCE_FICTION'.",
                    "The enum type 'Genre' in schema 'B' must define the value 'FANTASY'."
                ]
            }
        };
    }
}
