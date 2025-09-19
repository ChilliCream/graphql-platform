using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class InvalidShareableUsageRuleTests
{
    private static readonly object s_rule = new InvalidShareableUsageRule();
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
        Assert.True(_log.All(e => e.Code == "INVALID_SHAREABLE_USAGE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the field "orderStatus" on the "Order" object type is marked with
            // @shareable, which is allowed. It signals that this field can be served from multiple
            // schemas without creating a conflict.
            {
                [
                    """
                    type Order {
                        id: ID!
                        orderStatus: String @shareable
                        total: Float
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
            // In this example, the "InventoryItem" interface has a field "sku" marked with
            // @shareable, which is invalid usage. Marking an interface field as shareable leads to
            // an INVALID_SHAREABLE_USAGE error.
            {
                [
                    """
                    interface InventoryItem {
                        sku: ID! @shareable
                        name: String
                    }
                    """
                ],
                [
                    "The interface field 'InventoryItem.sku' in schema 'A' must not be marked as "
                    + "shareable."
                ]
            }
        };
    }
}
