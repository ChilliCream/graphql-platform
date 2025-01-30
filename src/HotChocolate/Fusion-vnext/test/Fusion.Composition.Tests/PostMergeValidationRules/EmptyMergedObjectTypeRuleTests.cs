using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedObjectTypeRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new EmptyMergedObjectTypeRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var merger = new SourceSchemaMerger(schemas);
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
        var merger = new SourceSchemaMerger(schemas);
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "EMPTY_MERGED_OBJECT_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the merged object type "Author" is valid. It includes all
            // fields from both source schemas, with "age" being hidden due to the @inaccessible
            // directive in one of the source schemas.
            {
                [
                    """
                    # Schema A
                    type Author {
                        name: String
                        age: Int @inaccessible
                    }
                    """,
                    """
                    # Schema B
                    type Author {
                        age: Int
                        registered: Boolean
                    }
                    """
                ]
            },
            // If the @inaccessible directive is applied to an object type itself, the entire merged
            // object type is excluded from the composite execution schema, and it is not required
            // to contain any fields.
            {
                [
                    """
                    # Schema A
                    type Author @inaccessible {
                        name: String
                        age: Int
                    }
                    """,
                    """
                    # Schema B
                    type Author {
                        registered: Boolean
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
            // This example demonstrates an invalid merged object type. In this case, "Author" is
            // defined in two source schemas, but all fields are marked as @inaccessible in at least
            // one of the source schemas, resulting in an empty merged object type.
            {
                [
                    """
                    # Schema A
                    type Author {
                        name: String @inaccessible
                        registered: Boolean
                    }
                    """,
                    """
                    # Schema B
                    type Author {
                        name: String
                        registered: Boolean @inaccessible
                    }
                    """
                ],
                [
                    "The merged object type 'Author' is empty."
                ]
            }
        };
    }
}
