using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class RequireInvalidFieldsTypeRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new RequireInvalidFieldsTypeRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "REQUIRE_INVALID_FIELDS_TYPE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @require directive’s "fields" argument is a valid
            // string and satisfies the rule.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        profile(name: String! @require(fields: "name")): Profile
                    }

                    type Profile {
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
            // Since "fields" is set to 123 (an integer) instead of a string, this violates the rule
            // and triggers a REQUIRE_INVALID_FIELDS_TYPE error.
            {
                [
                    """
                    type User @key(fields: "id") {
                        id: ID!
                        profile(name: String! @require(fields: 123)): Profile
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @require directive on argument 'User.profile(name:)' in schema 'A' must " +
                    "specify a string value for the 'fields' argument."
                ]
            }
        };
    }
}
