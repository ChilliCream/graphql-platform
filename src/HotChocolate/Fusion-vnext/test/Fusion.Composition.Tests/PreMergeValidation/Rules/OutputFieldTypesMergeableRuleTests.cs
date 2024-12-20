using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class OutputFieldTypesMergeableRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new OutputFieldTypesMergeableRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "OUTPUT_FIELD_TYPES_NOT_MERGEABLE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
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
                    "Field 'User.birthdate' has a different type shape in schema 'A' than it " +
                    "does in schema 'B'."
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
                    "Field 'User.tags' has a different type shape in schema 'A' than it does in " +
                    "schema 'B'."
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
                    "Field 'User.birthdate' has a different type shape in schema 'A' than it " +
                    "does in schema 'B'.",
                    "Field 'User.birthdate' has a different type shape in schema 'B' than it " +
                    "does in schema 'C'."
                ]
            }
        };
    }
}
