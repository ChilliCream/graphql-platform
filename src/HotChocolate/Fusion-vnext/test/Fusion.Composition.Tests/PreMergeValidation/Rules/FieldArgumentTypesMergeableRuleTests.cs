using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

public sealed class FieldArgumentTypesMergeableRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new FieldArgumentTypesMergeableRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "FIELD_ARGUMENT_TYPES_NOT_MERGEABLE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Arguments with the same type are mergeable.
            {
                [
                    """
                    type User {
                        field(argument: String): String
                    }
                    """,
                    """
                    type User {
                        field(argument: String): String
                    }
                    """
                ]
            },
            // Arguments that differ on nullability of an argument type are mergeable.
            {
                [
                    """
                    type User {
                        field(argument: String!): String
                    }
                    """,
                    """
                    type User {
                        field(argument: String): String
                    }
                    """
                ]
            },
            {
                [
                    """
                    type User {
                        field(argument: [String!]): String
                    }
                    """,
                    """
                    type User {
                        field(argument: [String]!): String
                    }
                    """,
                    """
                    type User {
                        field(argument: [String]): String
                    }
                    """
                ]
            },
            // The "User" type is inaccessible in schema B, so the argument will not be merged.
            {
                [
                    """
                    type User {
                        field(argument: String!): String
                    }
                    """,
                    """
                    type User @inaccessible {
                        field(argument: DateTime): String
                    }
                    """
                ]
            },
            // The "User" type is internal in schema B, so the argument will not be merged.
            {
                [
                    """
                    type User {
                        field(argument: String!): String
                    }
                    """,
                    """
                    type User @internal {
                        field(argument: DateTime): String
                    }
                    """
                ]
            },
            // The "field" field is inaccessible in schema B, so the argument will not be merged.
            {
                [
                    """
                    type User {
                        field(argument: String!): String
                    }
                    """,
                    """
                    type User {
                        field(argument: DateTime): String @inaccessible
                    }
                    """
                ]
            },
            // The "field" field is internal in schema B, so the argument will not be merged.
            {
                [
                    """
                    type User {
                        field(argument: String!): String
                    }
                    """,
                    """
                    type User {
                        field(argument: DateTime): String @internal
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
            // Arguments are not mergeable if the named types are different in kind or name.
            {
                [
                    """
                    type User {
                        field(argument: String!): String
                    }
                    """,
                    """
                    type User {
                        field(argument: DateTime): String
                    }
                    """
                ],
                [
                    "The argument 'User.field(argument:)' has a different type shape in schema " +
                    "'A' than it does in schema 'B'."
                ]
            },
            {
                [
                    """
                    type User {
                        field(argument: [String]): String
                    }
                    """,
                    """
                    type User {
                        field(argument: [DateTime]): String
                    }
                    """
                ],
                [
                    "The argument 'User.field(argument:)' has a different type shape in schema " +
                    "'A' than it does in schema 'B'."
                ]
            },
            // More than two schemas.
            {
                [
                    """
                    type User {
                        field(argument: [String]): String
                    }
                    """,
                    """
                    type User {
                        field(argument: [DateTime]): String
                    }
                    """,
                    """
                    type User {
                        field(argument: [Int]): String
                    }
                    """
                ],
                [
                    "The argument 'User.field(argument:)' has a different type shape in schema " +
                    "'A' than it does in schema 'B'.",

                    "The argument 'User.field(argument:)' has a different type shape in schema " +
                    "'B' than it does in schema 'C'."
                ]
            }
        };
    }
}
