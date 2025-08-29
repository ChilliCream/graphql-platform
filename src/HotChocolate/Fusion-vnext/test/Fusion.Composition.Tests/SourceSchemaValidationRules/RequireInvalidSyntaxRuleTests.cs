using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RequireInvalidSyntaxRuleTests
{
    private static readonly object s_rule = new RequireInvalidSyntaxRule();
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
        Assert.True(_log.All(e => e.Code == "REQUIRE_INVALID_SYNTAX"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @require directive’s "field" argument is a valid
            // selection map and satisfies the rule.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        profile(name: String @require(field: "name")): Profile
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
            // In the following example, the @require directive’s "field" argument has invalid
            // syntax because it is missing a closing brace.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        profile(name: String! @require(field: "{ name ")): Profile
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @require directive on argument 'User.profile(name:)' in schema 'A' "
                    + "contains invalid syntax in the 'field' argument."
                ]
            }
        };
    }
}
