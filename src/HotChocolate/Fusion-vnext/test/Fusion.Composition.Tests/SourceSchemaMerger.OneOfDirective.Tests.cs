namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerOneOfDirectiveTests : SourceSchemaMergerTestBase
{
    // Merge @oneOf directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_OneOfDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                input Foo @oneOf {
                    field: Int
                }

                "Some description"
                directive @oneOf on INPUT_OBJECT
                """,
                """
                # Schema B
                input Foo @oneOf {
                    field: Int
                }

                "Some description"
                directive @oneOf on INPUT_OBJECT
                """
            ],
            """
            input Foo
                @oneOf
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
            }
            """);
    }

    // Merge @oneOf directives without a definition in the source schemas.
    [Fact]
    public void Merge_OneOfDirectivesWhenDefinedInSourceSchema_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                input Foo @oneOf {
                    field: Int
                }
                """,
                """
                # Schema B
                input Foo @oneOf {
                    field: Int
                }
                """
            ],
            """
            input Foo
                @oneOf
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
            }
            """);
    }

    // Do not merge @oneOf directives when the definitions do not match the canonical definition.
    [Fact]
    public void Merge_OneOfDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                input Foo @oneOf {
                    field: Int
                }

                directive @oneOf repeatable on SCALAR
                """,
                """
                # Schema B
                input Foo @oneOf(id: 1) {
                    field: Int
                }

                directive @oneOf(id: Int) on INPUT_OBJECT
                """
            ],
            """
            input Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
            }
            """);
    }
}
