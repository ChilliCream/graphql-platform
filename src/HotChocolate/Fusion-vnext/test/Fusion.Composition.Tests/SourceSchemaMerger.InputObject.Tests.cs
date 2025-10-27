namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInputObjectTests : SourceSchemaMergerTestBase
{
    // Here, two "OrderInput" input types from different schemas are merged into a single composed
    // "OrderInput" type. Notice that only the fields present in both schemas are included. Although
    // "description" appears in Schema A and "total" appears in Schema B, neither field is defined
    // in both schemas; therefore, only "id" remains.
    [Fact]
    public void Merge_InputObjects_MatchesSnapshot()
    {
        AssertMatches(
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
                id: ID!
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
            }
            """);
    }

    // Another example demonstrates preserving descriptions during merging. In this case, the
    // description from the first schema is retained, while the fields are merged from both schemas
    // to create the final "OrderInput" type.
    [Fact]
    public void Merge_InputObjectsUsesFirstNonNullDescription_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // If any of the input objects is marked as @inaccessible, then the merged input object is also
    // marked as @inaccessible in the execution schema.
    [Fact]
    public void Merge_InaccessibleInputObject_MatchesSnapshot()
    {
        AssertMatches(
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
                @fusion__type(schema: A)
                @fusion__type(schema: B)
                @fusion__inaccessible {
                id: ID!
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
            }
            """);
    }
}
