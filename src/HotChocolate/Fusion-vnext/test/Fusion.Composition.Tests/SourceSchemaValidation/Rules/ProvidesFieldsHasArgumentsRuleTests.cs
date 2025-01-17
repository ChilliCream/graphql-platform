using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.SourceSchemaValidation.Rules;

namespace HotChocolate.Composition.SourceSchemaValidation.Rules;

public sealed class ProvidesFieldsHasArgumentsRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new ProvidesFieldsHasArgumentsRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

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
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "PROVIDES_FIELDS_HAS_ARGS"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        tags: [String]
                    }

                    type Article @key(fields: "id") {
                        id: ID!
                        author: User! @provides(fields: "tags")
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
            // This violates the rule because the "tags" field referenced in the "fields" argument
            // of the @provides directive is defined with arguments ("limit: UserType = ADMIN").
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        tags(limit: UserType = ADMIN): [String]
                    }

                    enum UserType {
                        REGULAR
                        ADMIN
                    }

                    type Article @key(fields: "id") {
                        id: ID!
                        author: User! @provides(fields: "tags")
                    }
                    """
                ],
                [
                    "The @provides directive on field 'Article.author' in schema 'A' references " +
                    "field 'User.tags', which must not have arguments."
                ]
            },
            // Nested field.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        info: UserInfo
                    }

                    type UserInfo {
                        tags(limit: UserType = ADMIN): [String]
                    }

                    enum UserType {
                        REGULAR
                        ADMIN
                    }

                    type Article @key(fields: "id") {
                        id: ID!
                        author: User! @provides(fields: "info { tags }")
                    }
                    """
                ],
                [
                    "The @provides directive on field 'Article.author' in schema 'A' references " +
                    "field 'UserInfo.tags', which must not have arguments."
                ]
            }
        };
    }
}
