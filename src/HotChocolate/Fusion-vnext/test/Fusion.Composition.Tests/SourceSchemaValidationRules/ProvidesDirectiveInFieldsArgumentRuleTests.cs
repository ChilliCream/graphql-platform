using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesDirectiveInFieldsArgumentRuleTests
{
    private static readonly object s_rule = new ProvidesDirectiveInFieldsArgumentRule();
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
        Assert.True(_log.All(e => e.Code == "PROVIDES_DIRECTIVE_IN_FIELDS_ARG"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "fields" argument of the @provides directive does not have any
            // directive applications, satisfying the rule.
            {
                [
                    """
                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile: Profile @provides(fields: "name")
                    }

                    type Profile {
                        id: ID!
                        name: String
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
            // In this example, the "fields" argument of the @provides directive has a directive
            // application @lowercase, which is not allowed.
            {
                [
                    """
                    directive @lowercase on FIELD_DEFINITION

                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile: Profile @provides(fields: "name @lowercase")
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.profile' in schema 'A' references "
                    + "field 'name', which must not include directive applications."
                ]
            },
            // Nested field.
            {
                [
                    """
                    directive @lowercase on FIELD_DEFINITION

                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile: Profile @provides(fields: "info { name @lowercase }")
                    }

                    type Profile {
                        id: ID!
                        info: ProfileInfo!
                    }

                    type ProfileInfo {
                        name: String
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.profile' in schema 'A' references "
                    + "field 'info.name', which must not include directive applications."
                ]
            },
            // Multiple fields.
            {
                [
                    """
                    directive @example on FIELD_DEFINITION

                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile: Profile @provides(fields: "id @example name @example")
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.profile' in schema 'A' references "
                    + "field 'id', which must not include directive applications.",

                    "The @provides directive on field 'User.profile' in schema 'A' references "
                    + "field 'name', which must not include directive applications."
                ]
            }
        };
    }
}
