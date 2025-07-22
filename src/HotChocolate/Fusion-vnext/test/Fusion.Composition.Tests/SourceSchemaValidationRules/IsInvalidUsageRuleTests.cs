using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class IsInvalidUsageRuleTests
{
    private static readonly object s_rule = new IsInvalidUsageRule();
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
        Assert.True(_log.All(e => e.Code == "IS_INVALID_USAGE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @is directive is applied to an argument declared on a
            // field with the @lookup directive, satisfying the rule.
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
            // In the following example, the @is directive is applied to an argument declared on a
            // field without the @lookup directive, violating the rule.
            {
                [
                    """
                    type Query {
                        personById(id: ID! @is(field: "id")): Person
                    }

                    type Person {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @is directive on argument 'Query.personById(id:)' in schema 'A' is "
                    + "invalid because the declaring field is not a lookup field."
                ]
            }
        };
    }
}
