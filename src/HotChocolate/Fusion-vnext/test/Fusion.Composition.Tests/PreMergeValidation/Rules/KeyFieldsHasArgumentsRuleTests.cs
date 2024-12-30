using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class KeyFieldsHasArgumentsRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator = new([new KeyFieldsHasArgumentsRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "KEY_FIELDS_HAS_ARGS"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "User" type has a valid @key directive that references the
            // argument-free fields "id" and "name".
            {
                [
                    """
                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
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
            // In this example, the @key directive references a field ("tags") that is defined with
            // arguments ("limit"), which is not allowed.
            {
                [
                    """
                    type User @key(fields: "id tags") {
                        id: ID!
                        tags(limit: Int = 10): [String]
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' references field " +
                    "'User.tags', which must not have arguments."
                ]
            },
            // Nested field.
            {
                [
                    """
                    type User @key(fields: "id info { tags }") {
                        id: ID!
                        info: UserInfo
                    }

                    type UserInfo {
                        tags(limit: Int = 10): [String]
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' references field " +
                    "'UserInfo.tags', which must not have arguments."
                ]
            },
            // Multiple keys.
            {
                [
                    """
                    type User @key(fields: "id") @key(fields: "tags") {
                        id(global: Boolean = true): ID!
                        tags(limit: Int = 10): [String]
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' references field " +
                    "'User.id', which must not have arguments.",

                    "A @key directive on type 'User' in schema 'A' references field " +
                    "'User.tags', which must not have arguments."
                ]
            }
        };
    }
}
