using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyInvalidFieldsTypeRuleTests
{
    private static readonly object s_rule = new KeyInvalidFieldsTypeRule();
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
        Assert.True(_log.All(e => e.Code == "KEY_INVALID_FIELDS_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the @key directive’s "fields" argument is the string "id uuid",
            // identifying two fields that form the object key. This usage is valid.
            {
                [
                    """
                    type User @key(fields: "id uuid") {
                        id: ID!
                        uuid: ID!
                        name: String
                    }

                    type Query {
                        users: [User]
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
            // Here, the "fields" argument is provided as a boolean (true) instead of a string. This
            // violates the directive requirement and triggers a KEY_INVALID_FIELDS_TYPE error.
            {
                [
                    """
                    type User @key(fields: true) {
                        id: ID
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' must specify a string value "
                    + "for the 'fields' argument."
                ]
            },
            // Multiple keys.
            {
                [
                    """
                    type User @key(fields: true) @key(fields: false) {
                        id: ID
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' must specify a string value "
                    + "for the 'fields' argument.",

                    "A @key directive on type 'User' in schema 'A' must specify a string value "
                    + "for the 'fields' argument."
                ]
            }
        };
    }
}
