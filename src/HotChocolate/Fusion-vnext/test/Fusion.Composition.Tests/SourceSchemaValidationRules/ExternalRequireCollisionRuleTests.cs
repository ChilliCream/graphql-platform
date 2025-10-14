using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalRequireCollisionRuleTests
{
    private static readonly object s_rule = new ExternalRequireCollisionRule();
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
        Assert.True(_log.All(e => e.Code == "EXTERNAL_REQUIRE_COLLISION"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, "title" has arguments annotated with @require in Schema B, but is
            // not marked as @external. This usage is valid.
            {
                [
                    """
                    # Source Schema A
                    type Book {
                        id: ID!
                        title: String
                        subtitle: String
                    }
                    """,
                    """
                    # Source Schema B
                    type Book {
                        id: ID!
                        title(subtitle: String @require(field: "subtitle")): String
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
            // The following example is invalid, since "title" is marked with @external and has an
            // argument that is annotated with @require. This conflict leads to an
            // EXTERNAL_REQUIRE_COLLISION error.
            {
                [
                    """
                    # Source Schema A
                    type Book {
                        id: ID!
                        title: String
                        subtitle: String
                    }
                    """,
                    """
                    # Source Schema B
                    type Book {
                        id: ID!
                        title(subtitle: String @require(field: "subtitle")): String @external
                    }
                    """
                ],
                [
                    "The external field 'Book.title' in schema 'B' must not have arguments that "
                    + "are annotated with the @require directive."
                ]
            }
        };
    }
}
