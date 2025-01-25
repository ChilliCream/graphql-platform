using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class LookupReturnsNonNullableTypeRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new LookupReturnsNonNullableTypeRule()]);

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
        Assert.True(result.IsSuccess);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "LOOKUP_RETURNS_NON_NULLABLE_TYPE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Warning));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, "userById" returns a nullable "User" type, aligning with the
            // recommendation.
            {
                [
                    """
                    type Query {
                        userById(id: ID!): User @lookup
                    }

                    type User {
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
            // Here, "userById" returns a non-nullable "User!", which does not align with the
            // recommendation that a @lookup field should have a nullable return type.
            {
                [
                    """
                    type Query {
                        userById(id: ID!): User! @lookup
                    }

                    type User {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The lookup field 'Query.userById' in schema 'A' should return a nullable type."
                ]
            }
        };
    }
}
