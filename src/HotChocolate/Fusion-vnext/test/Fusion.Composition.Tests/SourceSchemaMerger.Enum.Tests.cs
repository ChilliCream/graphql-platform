using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerEnumTests
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
            // Here, two "Status" enums from different schemas are merged into a single composed
            // "Status" enum. The enums are identical, so the composed enum exactly matches the
            // source enums.
            {
                [
                    """
                    # Schema A
                    enum Status {
                        ACTIVE
                        INACTIVE
                    }
                    """,
                    """
                    # Schema B
                    enum Status {
                        ACTIVE
                        INACTIVE
                    }
                    """
                ],
                """
                enum Status
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    ACTIVE
                        @fusion__enumValue(schema: A)
                        @fusion__enumValue(schema: B)
                    INACTIVE
                        @fusion__enumValue(schema: A)
                        @fusion__enumValue(schema: B)
                }
                """
            },
            // If the enums differ in their values, the source schemas must define their unique
            // values as @inaccessible to exclude them from the composed enum.
            {
                [
                    """
                    # Schema A
                    enum Status {
                        ACTIVE @inaccessible
                        INACTIVE
                    }
                    """,
                    """
                    # Schema B
                    enum Status {
                        PENDING @inaccessible
                        INACTIVE
                    }
                    """
                ],
                """
                enum Status
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    ACTIVE
                        @fusion__enumValue(schema: A)
                        @fusion__inaccessible
                    INACTIVE
                        @fusion__enumValue(schema: A)
                        @fusion__enumValue(schema: B)
                    PENDING
                        @fusion__enumValue(schema: B)
                        @fusion__inaccessible
                }
                """
            },
            // If any of the enums is marked as @inaccessible, then the merged enum is also marked
            // as @inaccessible in the execution schema.
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
                    enum Status @inaccessible {
                        INACTIVE
                    }
                    """
                ],
                """
                enum Status
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__inaccessible {
                    ACTIVE
                        @fusion__enumValue(schema: A)
                    INACTIVE
                        @fusion__enumValue(schema: B)
                }
                """
            },
            // The first non-null description encountered among the enums is used for the final
            // definition.
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
                    "The first non-null description."
                    enum Status {
                        ACTIVE
                    }
                    """
                ],
                """
                "The first non-null description."
                enum Status
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    ACTIVE
                        @fusion__enumValue(schema: A)
                        @fusion__enumValue(schema: B)
                }
                """
            }
        };
    }
}
