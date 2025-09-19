using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class OutputFieldTypesMergeableRuleTests
{
    private static readonly object s_rule = new OutputFieldTypesMergeableRule();
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
        Assert.True(_log.All(e => e.Code == "OUTPUT_FIELD_TYPES_NOT_MERGEABLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Fields with the same type are mergeable.
            {
                [
                    """
                    type User {
                        birthdate: String
                    }
                    """,
                    """
                    type User {
                        birthdate: String
                    }
                    """
                ]
            },
            // Fields with different nullability are mergeable, resulting in a merged field with a
            // nullable type.
            {
                [
                    """
                    type User {
                        birthdate: String!
                    }
                    """,
                    """
                    type User {
                        birthdate: String
                    }
                    """
                ]
            },
            {
                [
                    """
                    type User {
                        tags: [String!]
                    }
                    """,
                    """
                    type User {
                        tags: [String]!
                    }
                    """,
                    """
                    type User {
                        tags: [String]
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
            // Fields are not mergeable if the named types are different in kind or name.
            {
                [
                    """
                    type User {
                        birthdate: String!
                    }
                    """,
                    """
                    type User {
                        birthdate: DateTime!
                    }
                    """
                ],
                [
                    "The output field 'User.birthdate' has a different type shape in schema 'A' "
                    + "than it does in schema 'B'."
                ]
            },
            {
                [
                    """
                    type User {
                        tags: [Tag]
                    }

                    type Tag {
                        value: String
                    }
                    """,
                    """
                    type User {
                        tags: [Tag]
                    }

                    scalar Tag
                    """
                ],
                [
                    "The output field 'User.tags' has a different type shape in schema 'A' than "
                    + "it does in schema 'B'."
                ]
            },
            // More than two schemas.
            {
                [
                    """
                    type User {
                        birthdate: String!
                    }
                    """,
                    """
                    type User {
                        birthdate: DateTime!
                    }
                    """,
                    """
                    type User {
                        birthdate: Int!
                    }
                    """
                ],
                [
                    "The output field 'User.birthdate' has a different type shape in schema 'A' "
                    + "than it does in schema 'B'.",

                    "The output field 'User.birthdate' has a different type shape in schema 'B' "
                    + "than it does in schema 'C'."
                ]
            }
        };
    }
}
