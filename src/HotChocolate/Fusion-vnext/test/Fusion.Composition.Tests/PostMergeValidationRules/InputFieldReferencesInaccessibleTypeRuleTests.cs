using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class InputFieldReferencesInaccessibleTypeRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new InputFieldReferencesInaccessibleTypeRule();
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
        Assert.True(_log.All(e => e.Code == "INPUT_FIELD_REFERENCES_INACCESSIBLE_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // A valid case where a public input field references another public input type.
            {
                [
                    """
                    # Schema A
                    input Input1 {
                        field1: String!
                        field2: Input2
                    }

                    input Input2 {
                        field3: String
                    }
                    """,
                    """
                    # Schema B
                    input Input2 {
                        field3: String
                    }
                    """
                ]
            },
            // Another valid case is where the field is not exposed in the composed schema.
            {
                [
                    """
                    # Schema A
                    input Input1 {
                        field1: String!
                        field2: Input2 @inaccessible
                    }

                    input Input2 {
                        field3: String
                    }
                    """,
                    """
                    # Schema B
                    input Input2 @inaccessible {
                        field3: String
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
            // An invalid case is when an input field references an inaccessible type.
            {
                [
                    """
                    # Schema A
                    input Input1 {
                        field1: String!
                        field2: Input2!
                    }

                    input Input2 {
                        field3: String
                    }
                    """,
                    """
                    # Schema B
                    input Input2 @inaccessible {
                        field3: String
                    }
                    """
                ],
                [
                    "The merged input field 'field2' in type 'Input1' cannot reference the " +
                    "inaccessible type 'Input2'."
                ]
            }
        };
    }
}
