using HotChocolate.Fusion.Options;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerEnumValueTests : CompositionTestBase
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
            // If any of the enum values is marked as @inaccessible, then the merged enum value is
            // also marked as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    enum Status {
                        ACTIVE
                    }
                    """,
                    """
                    # Schema B
                    enum Status {
                        INACTIVE @inaccessible
                    }
                    """
                ],
                """
                enum Status
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    ACTIVE
                        @fusion__enumValue(schema: A)
                    INACTIVE
                        @fusion__enumValue(schema: B)
                        @fusion__inaccessible
                }
                """
            },
            // The first non-null description encountered among the enum values is used for the
            // final definition.
            {
                [
                    """
                    # Schema A
                    enum Status {
                        ACTIVE
                    }
                    """,
                    """
                    # Schema B
                    enum Status {
                        "The first non-null description."
                        ACTIVE
                    }
                    """
                ],
                """
                enum Status
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    "The first non-null description."
                    ACTIVE
                        @fusion__enumValue(schema: A)
                        @fusion__enumValue(schema: B)
                }
                """
            }
        };
    }
}
