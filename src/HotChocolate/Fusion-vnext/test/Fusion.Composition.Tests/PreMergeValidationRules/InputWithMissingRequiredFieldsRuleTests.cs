using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputWithMissingRequiredFieldsRuleTests
{
    private static readonly object s_rule = new InputWithMissingRequiredFieldsRule();
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
        Assert.True(_log.All(e => e.Code == "INPUT_WITH_MISSING_REQUIRED_FIELDS"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
                    "The input type 'BookFilter' in schema 'B' must define the required field "
                    + "'title'."
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
                    "The input type 'BookFilter' in schema 'A' must define the required field "
                    + "'author'.",

                    "The input type 'BookFilter' in schema 'B' must define the required field "
                    + "'title'."
                ]
            }
        };
    }
}
