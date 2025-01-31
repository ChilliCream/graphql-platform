using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class LookupReturnsListRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new LookupReturnsListRule();
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
        Assert.True(_log.All(e => e.Code == "LOOKUP_RETURNS_LIST"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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
