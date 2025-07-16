using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputFieldTypesMergeableRuleTests
{
    private static readonly object s_rule = new InputFieldTypesMergeableRule();
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
        Assert.True(_log.All(e => e.Code == "INPUT_FIELD_TYPES_NOT_MERGEABLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the field "name" in "AuthorInput" has compatible types across source
            // schemas, making them mergeable.
            {
                [
                    """
                    input AuthorInput {
                        name: String!
                    }
                    """,
                    """
                    input AuthorInput {
                        name: String
                    }
                    """
                ]
            },
            // The following example shows that fields are mergeable if they have different
            // nullability but the named type is the same and the list structure is the same.
            {
                [
                    """
                    input AuthorInput {
                        tags: [String!]
                    }
                    """,
                    """
                    input AuthorInput {
                        tags: [String]!
                    }
                    """,
                    """
                    input AuthorInput {
                        tags: [String]
                    }
                    """
                ]
            },
            // Multiple input fields.
            {
                [
                    """
                    input AuthorInput {
                        name: String!
                        tags: [String!]
                        birthdate: DateTime
                    }
                    """,
                    """
                    input AuthorInput {
                        name: String
                        tags: [String]!
                        birthdate: DateTime!
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
            // In this example, the field "birthdate" on "AuthorInput" is not mergeable as the field
            // has different named types ("String" and "DateTime") across source schemas.
            {
                [
                    """
                    input AuthorInput {
                        birthdate: String!
                    }
                    """,
                    """
                    input AuthorInput {
                        birthdate: DateTime!
                    }
                    """
                ],
                [
                    "The input field 'AuthorInput.birthdate' has a different type shape in "
                    + "schema 'A' than it does in schema 'B'."
                ]
            },
            // List versus non-list.
            {
                [
                    """
                    input AuthorInput {
                        birthdate: String!
                    }
                    """,
                    """
                    input AuthorInput {
                        birthdate: [String!]
                    }
                    """
                ],
                [
                    "The input field 'AuthorInput.birthdate' has a different type shape in "
                    + "schema 'A' than it does in schema 'B'."
                ]
            }
        };
    }
}
