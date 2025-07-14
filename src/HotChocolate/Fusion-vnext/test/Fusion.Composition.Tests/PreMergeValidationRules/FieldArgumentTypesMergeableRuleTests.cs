using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class FieldArgumentTypesMergeableRuleTests
{
    private static readonly object s_rule = new FieldArgumentTypesMergeableRule();
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
        Assert.True(_log.All(e => e.Code == "FIELD_ARGUMENT_TYPES_NOT_MERGEABLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
                    "The argument 'User.field(argument:)' has a different type shape in schema "
                    + "'A' than it does in schema 'B'."
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
                    "The argument 'User.field(argument:)' has a different type shape in schema "
                    + "'A' than it does in schema 'B'."
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
                    "The argument 'User.field(argument:)' has a different type shape in schema "
                    + "'A' than it does in schema 'B'.",

                    "The argument 'User.field(argument:)' has a different type shape in schema "
                    + "'B' than it does in schema 'C'."
                ]
            }
        };
    }
}
