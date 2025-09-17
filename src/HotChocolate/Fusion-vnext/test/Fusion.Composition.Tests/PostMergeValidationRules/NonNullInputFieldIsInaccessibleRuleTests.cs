using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class NonNullInputFieldIsInaccessibleRuleTests
{
    private static readonly object s_rule = new NonNullInputFieldIsInaccessibleRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedTypes = false });
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

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
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedTypes = false });
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "NON_NULL_INPUT_FIELD_IS_INACCESSIBLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // The following is valid because the "age" field, although @inaccessible in one source
            // schema, is nullable and can be safely omitted in the final schema without breaking
            // any mandatory input requirement.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        author: String!
                        age: Int @inaccessible
                    }
                    """,
                    """
                    input BookFilter {
                        author: String!
                        age: Int
                    }
                    """
                ]
            },
            // Another valid case is when a nullable input field is removed during merging.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        author: String!
                        age: Int
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        author: String!
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
            // An invalid case is when a non-null input field is inaccessible.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        author: String!
                        age: Int!
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        author: String!
                        age: Int @inaccessible
                    }
                    """
                ],
                [
                    "The non-null input field 'BookFilter.age' in schema 'A' must be accessible "
                    + "in the composed schema."
                ]
            },
            // Another invalid case is when a non-null input field is removed during merging.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        author: String!
                        age: Int!
                    }
                    """,
                    """
                    input BookFilter {
                        author: String!
                    }
                    """
                ],
                [
                    "The non-null input field 'BookFilter.age' in schema 'A' must be accessible "
                    + "in the composed schema."
                ]
            }
        };
    }
}
