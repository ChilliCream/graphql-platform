using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class OutputFieldTypesMergeableRuleTests
{
    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new OutputFieldTypesMergeableRule()]);

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
        var preMergeValidator = new PreMergeValidator([new OutputFieldTypesMergeableRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(log);
        Assert.Equal("OUTPUT_FIELD_TYPES_NOT_MERGEABLE", log.First().Code);
        Assert.Equal(LogSeverity.Error, log.First().Severity);
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

    public static TheoryData<string[]> InvalidExamplesData()
    {
        return new TheoryData<string[]>
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
                ]
            }
        };
    }
}
