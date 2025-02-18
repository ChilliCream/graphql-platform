using HotChocolate.Fusion.Options;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerScalarTests : CompositionTestBase
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string[] sdl, string executionSchema)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }

    public static TheoryData<string[], string> ExamplesData()
    {
        return new TheoryData<string[], string>
        {
            // Here, two "Date" scalar types from different schemas are merged into a single
            // composed "Date" scalar type.
            {
                [
                    """
                    # Schema A
                    scalar Date
                    """,
                    """
                    # Schema B
                    "A scalar representing a calendar date."
                    scalar Date
                    """
                ],
                """
                "A scalar representing a calendar date."
                scalar Date
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                """
            },
            // If any of the scalars is marked as @inaccessible, then the merged scalar is also
            // marked as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    scalar Date
                    """,
                    """
                    # Schema B
                    scalar Date @inaccessible
                    """
                ],
                """
                scalar Date
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__inaccessible
                """
            },
            // The final description is determined by the first non-null description found in the
            // list of scalars.
            {
                [
                    """
                    # Schema A
                    "The first non-null description."
                    scalar Date
                    """,
                    """
                    # Schema B
                    "A scalar representing a calendar date."
                    scalar Date
                    """
                ],
                """
                "The first non-null description."
                scalar Date
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                """
            },
            // If no descriptions are found, the final description is null.
            {
                [
                    """
                    # Schema A
                    scalar Date
                    """,
                    """
                    # Schema B
                    scalar Date
                    """
                ],
                """
                scalar Date
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                """
            }
        };
    }
}
