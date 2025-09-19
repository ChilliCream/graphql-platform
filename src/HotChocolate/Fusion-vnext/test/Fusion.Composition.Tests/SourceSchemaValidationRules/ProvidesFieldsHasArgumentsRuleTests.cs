using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesFieldsHasArgumentsRuleTests
{
    private static readonly object s_rule = new ProvidesFieldsHasArgumentsRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

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
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "PROVIDES_FIELDS_HAS_ARGS"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
                    "The @provides directive on field 'Article.author' in schema 'A' references "
                    + "field 'User.tags', which must not have arguments."
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
                    "The @provides directive on field 'Article.author' in schema 'A' references "
                    + "field 'UserInfo.tags', which must not have arguments."
                ]
            }
        };
    }
}
