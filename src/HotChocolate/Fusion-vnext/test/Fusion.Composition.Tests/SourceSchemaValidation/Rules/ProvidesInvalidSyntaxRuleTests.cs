using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.SourceSchemaValidation.Rules;

namespace HotChocolate.Composition.SourceSchemaValidation.Rules;

public sealed class ProvidesInvalidSyntaxRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new ProvidesInvalidSyntaxRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "PROVIDES_INVALID_SYNTAX"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
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
                    "The @provides directive on field 'User.address' in schema 'A' contains " +
                    "invalid syntax in the 'fields' argument."
                ]
            }
        };
    }
}
