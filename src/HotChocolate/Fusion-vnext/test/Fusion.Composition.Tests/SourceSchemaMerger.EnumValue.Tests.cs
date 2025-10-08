using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerEnumValueTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string[] sdl, string executionSchema)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions
            {
                RemoveUnreferencedTypes = false,
                AddFusionDefinitions = false
            });

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
            },
            // Even if an enum value is only @deprecated in one source schema,
            // the composite enum value is marked as @deprecated.
            {
                [
                    """
                    # Schema A
                    enum Status {
                        ACTIVE @deprecated(reason: "Some reason")
                    }
                    """,
                    """
                    # Schema B
                    enum Status {
                        ACTIVE
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
                        @deprecated(reason: "Some reason")
                }
                """
            },
            // If the same enum value is @deprecated in multiple source schemas,
            // the first non-null deprecation message is chosen.
            {
                [
                    """
                    # Schema A
                    enum Status {
                        ACTIVE @deprecated(reason: "Some reason")
                    }
                    """,
                    """
                    # Schema B
                    enum Status {
                        ACTIVE @deprecated(reason: "Another reason")
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
                        @deprecated(reason: "Some reason")
                }
                """
            },
            // If the an enum value is deprecated without a deprecation reason,
            // a default reason is inserted to be compatible with the latest spec.
            {
                [
                    """
                    # Schema A
                    enum Status {
                        ACTIVE @deprecated
                    }
                    """,
                    """
                    # Schema B
                    enum Status {
                        ACTIVE @deprecated
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
                        @deprecated(reason: "No longer supported.")
                }
                """
            }
        };
    }
}
