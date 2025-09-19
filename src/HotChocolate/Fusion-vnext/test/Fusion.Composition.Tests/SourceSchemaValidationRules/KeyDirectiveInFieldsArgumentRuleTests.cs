using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyDirectiveInFieldsArgumentRuleTests
{
    private static readonly object s_rule = new KeyDirectiveInFieldsArgumentRule();
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
        Assert.True(_log.All(e => e.Code == "KEY_DIRECTIVE_IN_FIELDS_ARG"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "fields" argument of the @key directive does not include any
            // directive applications, satisfying the rule.
            {
                [
                    """
                    type User @key(fields: "id name") {
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
            // In this example, the "fields" argument of the @key directive includes a directive
            // application @lowercase, which is not allowed.
            {
                [
                    """
                    directive @lowercase on FIELD_DEFINITION

                    type User @key(fields: "id name @lowercase") {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' references field 'name', "
                    + "which must not include directive applications."
                ]
            },
            // In this example, the "fields" argument includes a directive application @lowercase
            // nested inside the selection set, which is also invalid.
            {
                [
                    """
                    directive @lowercase on FIELD_DEFINITION

                    type User @key(fields: "id name { firstName @lowercase }") {
                        id: ID!
                        name: FullName
                    }

                    type FullName {
                        firstName: String
                        lastName: String
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' references field "
                    + "'name.firstName', which must not include directive applications."
                ]
            },
            // Multiple keys.
            {
                [
                    """
                    directive @example on FIELD_DEFINITION

                    type User @key(fields: "id @example") @key(fields: "name @example") {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "A @key directive on type 'User' in schema 'A' references field 'id', "
                    + "which must not include directive applications.",

                    "A @key directive on type 'User' in schema 'A' references field 'name', "
                    + "which must not include directive applications."
                ]
            }
        };
    }
}
