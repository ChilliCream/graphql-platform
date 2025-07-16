using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class OverrideFromSelfRuleTests
{
    private static readonly object s_rule = new OverrideFromSelfRule();
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
        Assert.True(_log.All(e => e.Code == "OVERRIDE_FROM_SELF"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, Schema B overrides the field "amount" from Schema A. The
            // two schema names are different, so no error is raised.
            {
                [
                    """
                    # Source Schema A
                    type Bill {
                        id: ID!
                        amount: Int
                    }
                    """,
                    """
                    # Source Schema B
                    type Bill {
                        id: ID!
                        amount: Int @override(from: "A")
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
            // In the following example, the local schema is also "A", and the "from" argument is
            // "A". Overriding a field from the same schema is not allowed, causing an
            // OVERRIDE_FROM_SELF error.
            {
                [
                    """
                    # Source Schema A (named "A")
                    type Bill {
                        id: ID!
                        amount: Int @override(from: "A")
                    }
                    """
                ],
                [
                    "The @override directive on field 'Bill.amount' in schema 'A' must not "
                    + "reference the same schema."
                ]
            }
        };
    }
}
