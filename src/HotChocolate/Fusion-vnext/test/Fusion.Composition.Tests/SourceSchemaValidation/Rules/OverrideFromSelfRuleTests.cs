using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class OverrideFromSelfRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new OverrideFromSelfRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "OVERRIDE_FROM_SELF"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
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
                    "The @override directive on field 'Bill.amount' in schema 'A' must not " +
                    "reference the same schema."
                ]
            }
        };
    }
}
