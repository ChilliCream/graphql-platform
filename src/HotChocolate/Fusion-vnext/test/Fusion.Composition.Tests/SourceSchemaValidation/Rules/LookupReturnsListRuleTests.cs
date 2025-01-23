using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class LookupReturnsListRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new LookupReturnsListRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "LOOKUP_RETURNS_LIST"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, "userById" returns a "User" object, satisfying the requirement.
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
            // Here, "usersByIds" returns a list of "User" objects, which violates the requirement
            // that a @lookup field must return a single object.
            {
                [
                    """
                    type Query {
                        usersByIds(ids: [ID!]!): [User!] @lookup
                    }

                    type User {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The lookup field 'Query.usersByIds' in schema 'A' must not return a list."
                ]
            },
            // Non-null list.
            {
                [
                    """
                    type Query {
                        usersByIds(ids: [ID!]!): [User!]! @lookup
                    }

                    type User {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The lookup field 'Query.usersByIds' in schema 'A' must not return a list."
                ]
            }
        };
    }
}
