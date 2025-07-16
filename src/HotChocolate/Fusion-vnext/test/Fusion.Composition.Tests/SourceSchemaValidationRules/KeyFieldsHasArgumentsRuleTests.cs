using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyFieldsHasArgumentsRuleTests
{
    private static readonly object s_rule = new KeyFieldsHasArgumentsRule();
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
        Assert.True(_log.All(e => e.Code == "KEY_FIELDS_HAS_ARGS"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
                    "A @key directive on type 'User' in schema 'A' references field "
                    + "'User.tags', which must not have arguments."
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
                    "A @key directive on type 'User' in schema 'A' references field "
                    + "'UserInfo.tags', which must not have arguments."
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
                    "A @key directive on type 'User' in schema 'A' references field "
                    + "'User.id', which must not have arguments.",

                    "A @key directive on type 'User' in schema 'A' references field "
                    + "'User.tags', which must not have arguments."
                ]
            }
        };
    }
}
