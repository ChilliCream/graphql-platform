using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class OverrideOnInterfaceRuleTests
{
    private static readonly object s_rule = new OverrideOnInterfaceRule();
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
        Assert.True(_log.All(e => e.Code == "OVERRIDE_ON_INTERFACE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, @override is used on a field of an object type, ensuring that the
            // field definition is concrete and can be reassigned to another schema.
            {
                [
                    """
                    # Source Schema A
                    type Order {
                        id: ID!
                        amount: Int
                    }
                    """,
                    """
                    # Source Schema B
                    type Order {
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
            // In the following example, "Bill.amount" is declared on an interface type and
            // annotated with @override. This violates the rule because the interface field itself
            // is not eligible for ownership transfer. The composition fails with an
            // OVERRIDE_ON_INTERFACE error.
            {
                [
                    """
                    # Source Schema A
                    interface Bill {
                        id: ID!
                        amount: Int @override(from: "B")
                    }
                    """
                ],
                [
                    "The interface field 'Bill.amount' in schema 'A' must not be annotated with "
                    + "the @override directive."
                ]
            }
        };
    }
}
