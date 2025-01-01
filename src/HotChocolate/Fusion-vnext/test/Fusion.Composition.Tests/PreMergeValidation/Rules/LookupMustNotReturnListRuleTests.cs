using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class LookupMustNotReturnListRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new LookupMustNotReturnListRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _preMergeValidator.Validate(context);

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
        var result = _preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "LOOKUP_MUST_NOT_RETURN_LIST"));
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
                    "Lookup field 'Query.usersByIds' in schema 'A' must not return a list."
                ]
            }
        };
    }
}
