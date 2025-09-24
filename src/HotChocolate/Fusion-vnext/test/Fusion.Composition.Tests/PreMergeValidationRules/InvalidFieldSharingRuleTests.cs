using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InvalidFieldSharingRuleTests
{
    private static readonly object s_rule = new InvalidFieldSharingRule();
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
        Assert.True(_log.All(e => e.Code == "INVALID_FIELD_SHARING"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "User" type field "fullName" is marked as shareable in both
            // schemas, allowing them to serve consistent data for that field without conflict.
            {
                [
                    """
                    # Schema A
                    type User @key(fields: "id") {
                        id: ID!
                        username: String
                        fullName: String @shareable
                    }
                    """,
                    """
                    # Schema B
                    type User @key(fields: "id") {
                        id: ID!
                        fullName: String @shareable
                        email: String
                    }
                    """
                ]
            },
            // In the following example, "User.fullName" is overridden in one schema and therefore
            // the field can be defined in the other schema without being marked as @shareable.
            {
                [
                    """
                    # Schema A
                    type User @key(fields: "id") {
                        id: ID!
                        fullName: String @override(from: "B")
                    }
                    """,
                    """
                    # Schema B
                    type User @key(fields: "id") {
                        id: ID!
                        fullName: String
                    }
                    """
                ]
            },
            // In the following example, "User.fullName" is marked as @external in one schema and
            // therefore the field can be defined in the other schema without being marked as
            // @shareable.
            {
                [
                    """
                    # Schema A
                    type User @key(fields: "id") {
                        id: ID!
                        fullName: String @external
                    }
                    """,
                    """
                    # Schema B
                    type User @key(fields: "id") {
                        id: ID!
                        fullName: String
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
            // In the following example, "User.fullName" is non-shareable but is defined and
            // resolved by two different schemas, resulting in an INVALID_FIELD_SHARING error.
            {
                [
                    """
                    # Schema A
                    type User @key(fields: "id") {
                        id: ID!
                        fullName: String
                    }
                    """,
                    """
                    # Schema B
                    type User @key(fields: "id") {
                        id: ID!
                        fullName: String
                    }
                    """
                ],
                [
                    "The field 'User.fullName' in schema 'A' must be shareable.",
                    "The field 'User.fullName' in schema 'B' must be shareable."
                ]
            }
        };
    }
}
