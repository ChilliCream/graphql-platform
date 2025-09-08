using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesInvalidFieldsRuleTests
{
    private static readonly object s_rule = new ProvidesInvalidFieldsRule();
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
        Assert.True(_log.All(e => e.Code == "PROVIDES_INVALID_FIELDS"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @provides directive references a valid field
            // ("hobbies") on the "UserDetails" type.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        details: UserDetails @provides(fields: "hobbies")
                    }

                    type UserDetails {
                        hobbies: [String]
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
            // In the following example, the @provides directive specifies a field named
            // "unknownField" which is not defined on "UserDetails". This raises a
            // PROVIDES_INVALID_FIELDS error.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        details: UserDetails @provides(fields: "unknownField")
                    }

                    type UserDetails {
                        hobbies: [String]
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.details' in schema 'A' specifies an "
                    + "invalid field selection."
                ]
            }
        };
    }
}
