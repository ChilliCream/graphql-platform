using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class InputFieldTypesMergeableRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new InputFieldTypesMergeableRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "INPUT_FIELD_TYPES_NOT_MERGEABLE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
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
            },
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
                    "Input field 'AuthorInput.birthdate' has a different type shape in schema " +
                    "'A' than it does in schema 'B'."
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
                    "Input field 'AuthorInput.birthdate' has a different type shape in schema " +
                    "'A' than it does in schema 'B'."
                ]
            }
        };
    }
}
