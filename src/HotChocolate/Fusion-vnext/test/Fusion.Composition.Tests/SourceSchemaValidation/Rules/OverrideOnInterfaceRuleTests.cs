using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.SourceSchemaValidation.Rules;

namespace HotChocolate.Composition.SourceSchemaValidation.Rules;

public sealed class OverrideOnInterfaceRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new OverrideOnInterfaceRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(context.Log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "OVERRIDE_ON_INTERFACE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
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
                    "The interface field 'Bill.amount' in schema 'A' must not be annotated with " +
                    "the @override directive."
                ]
            }
        };
    }
}
