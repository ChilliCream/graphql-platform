using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesInvalidSyntaxRuleTests
{
    private static readonly object s_rule = new ProvidesInvalidSyntaxRule();
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
        Assert.True(_log.All(e => e.Code == "PROVIDES_INVALID_SYNTAX"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the @provides directive’s "fields" argument is a valid selection set.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        address: Address @provides(fields: "street city")
                    }

                    type Address {
                        street: String
                        city: String
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
            // In this example, the "fields" argument is missing a closing brace. It cannot be
            // parsed as a valid GraphQL selection set, triggering a PROVIDES_INVALID_SYNTAX error.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        address: Address @provides(fields: "{ street city ")
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.address' in schema 'A' contains "
                    + "invalid syntax in the 'fields' argument."
                ]
            }
        };
    }
}
