using HotChocolate.Fusion.Options;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInputFieldTests : CompositionTestBase
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
            // Suppose we have two input type definitions for the same "OrderFilter" input field,
            // defined in separate schemas. In the final schema, "minTotal" is defined using the
            // most restrictive type (Int!), has a default value of 0, and includes the description
            // from the original field in Schema A.
            {
                [
                    """"
                    # Schema A
                    input OrderFilter {
                        """
                        Filter by the minimum order total
                        """
                        minTotal: Int = 0
                    }
                    """",
                    """
                    # Schema B
                    input OrderFilter {
                        minTotal: Int!
                    }
                    """
                ],
                """
                input OrderFilter
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    "Filter by the minimum order total"
                    minTotal: Int! = 0
                        @fusion__inputField(schema: A, sourceType: "Int")
                        @fusion__inputField(schema: B)
                }
                """
            },
            // If any of the input fields is marked as @inaccessible, then the merged input field is
            // also marked as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    input OrderFilter {
                        minTotal: Int
                    }
                    """,
                    """
                    # Schema B
                    input OrderFilter {
                        minTotal: Int @inaccessible
                    }
                    """
                ],
                """
                input OrderFilter
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    minTotal: Int
                        @inaccessible
                        @fusion__inputField(schema: A)
                        @fusion__inputField(schema: B)
                }
                """
            },
            // If no description is found, the merged field will have no description.
            {
                [
                    """
                    # Schema A
                    input OrderFilter {
                        minTotal: Int
                    }
                    """,
                    """
                    # Schema B
                    input OrderFilter {
                        minTotal: Int
                    }
                    """
                ],
                """
                input OrderFilter
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    minTotal: Int
                        @fusion__inputField(schema: A)
                        @fusion__inputField(schema: B)
                }
                """
            }
        };
    }
}
