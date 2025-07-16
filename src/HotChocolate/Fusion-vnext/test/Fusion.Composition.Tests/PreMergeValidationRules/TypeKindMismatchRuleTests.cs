using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class TypeKindMismatchRuleTests
{
    private static readonly object s_rule = new TypeKindMismatchRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new PreMergeValidator(schemas, s_rules, _log);

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
        var validator = new PreMergeValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "TYPE_KIND_MISMATCH"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // All schemas agree that "User" is an object type.
            {
                [
                    """
                    # Schema A
                    type User {
                        id: ID!
                        name: String
                    }
                    """,
                    """
                    # Schema B
                    type User {
                        id: ID!
                        email: String
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
            // In the following example, "User" is defined as an object type in one of the schemas
            // and as an interface in another. This violates the rule and results in a
            // TYPE_KIND_MISMATCH error.
            {
                [
                    """
                    # Schema A
                    type User {
                        id: ID!
                        name: String
                    }
                    """,
                    """
                    # Schema B
                    interface User {
                        id: ID!
                        friends: [User!]!
                    }
                    """
                ],
                [
                    "The type 'User' has a different kind in schema 'A' (Object) than it does in "
                    + "schema 'B' (Interface)."
                ]
            }
        };
    }
}
