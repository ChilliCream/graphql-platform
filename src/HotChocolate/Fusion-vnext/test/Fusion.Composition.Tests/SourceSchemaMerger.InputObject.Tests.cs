using HotChocolate.Fusion.Options;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInputObjectTests : CompositionTestBase
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
        var result = merger.MergeSchemas();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }

    public static TheoryData<string[], string> ExamplesData()
    {
        return new TheoryData<string[], string>
        {
            // In this example, the "OrderInput" type from two schemas is merged. The "id" field is
            // shared across both schemas, while "description" and "total" fields are contributed by
            // the individual source schemas. The resulting composed type includes all fields.
            {
                [
                    """
                    # Schema A
                    input OrderInput {
                        id: ID!
                        description: String
                    }
                    """,
                    """
                    # Schema B
                    input OrderInput {
                        id: ID!
                        total: Float
                    }
                    """
                ],
                """
                input OrderInput
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    description: String
                        @fusion__inputField(schema: A)
                    id: ID!
                        @fusion__inputField(schema: A)
                        @fusion__inputField(schema: B)
                    total: Float
                        @fusion__inputField(schema: B)
                }
                """
            },
            // Another example demonstrates preserving descriptions during merging. In this case,
            // the description from the first schema is retained, while the fields are merged from
            // both schemas to create the final "OrderInput" type.
            {
                [
                    """"
                    # Schema A
                    """
                    First Description
                    """
                    input OrderInput {
                        id: ID!
                    }
                    """",
                    """"
                    # Schema B
                    """
                    Second Description
                    """
                    input OrderInput {
                        id: ID!
                    }
                    """"
                ],
                """
                "First Description"
                input OrderInput
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__inputField(schema: A)
                        @fusion__inputField(schema: B)
                }
                """
            },
            // If any of the input objects is marked as @inaccessible, then the merged input object
            // is also marked as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    input OrderInput {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    input OrderInput @inaccessible {
                        id: ID!
                    }
                    """
                ],
                """
                input OrderInput
                    @inaccessible
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__inputField(schema: A)
                        @fusion__inputField(schema: B)
                }
                """
            }
        };
    }
}
