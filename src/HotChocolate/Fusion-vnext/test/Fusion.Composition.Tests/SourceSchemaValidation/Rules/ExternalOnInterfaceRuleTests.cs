using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class ExternalOnInterfaceRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new ExternalOnInterfaceRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "EXTERNAL_ON_INTERFACE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the interface "Node" merely describes the field "id". Object types "User" and
            // "Product" implement and resolve "id". No @external usage occurs on the interface
            // itself, so no error is triggered.
            {
                [
                    """
                    interface Node {
                        id: ID!
                    }

                    type User implements Node {
                        id: ID!
                        name: String
                    }

                    type Product implements Node {
                        id: ID!
                        price: Int
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
            // Since "id" is declared on an interface and marked with @external, the composition
            // fails with EXTERNAL_ON_INTERFACE. An interface does not own the concrete field
            // resolution, so it is invalid to mark any of its fields as external.
            {
                [
                    """
                    interface Node {
                        id: ID! @external
                    }
                    """
                ],
                [
                    "The interface field 'Node.id' in schema 'A' must not be marked as external."
                ]
            }
        };
    }
}
