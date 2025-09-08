using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class IsInvalidFieldTypeRuleTests
{
    private static readonly object s_rule = new IsInvalidFieldTypeRule();
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
        Assert.True(_log.All(e => e.Code == "IS_INVALID_FIELD_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @is directive’s "field" argument is a valid string
            // and satisfies the rule.
            {
                [
                    """
                    type Query {
                        personById(id: ID! @is(field: "id")): Person @lookup
                    }

                    type Person {
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
            // Since "field" is set to 123 (an integer) instead of a string, this violates the rule
            // and triggers an IS_INVALID_FIELD_TYPE error.
            {
                [
                    """
                    type Query {
                        personById(id: ID! @is(field: 123)): Person @lookup
                    }

                    type Person {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @is directive on argument 'Query.personById(id:)' in schema 'A' must "
                    + "specify a string value for the 'field' argument."
                ]
            }
        };
    }
}
