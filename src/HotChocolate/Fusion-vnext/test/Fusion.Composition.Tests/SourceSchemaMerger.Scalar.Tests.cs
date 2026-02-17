namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerScalarTests : SourceSchemaMergerTestBase
{
    // Here, two "Date" scalar types from different schemas are merged into a single composed "Date"
    // scalar type.
    [Fact]
    public void Merge_Scalars_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // If any of the scalars is marked as @inaccessible, then the merged scalar is also marked as
    // @inaccessible in the execution schema.
    [Fact]
    public void Merge_InaccessibleScalar_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // The final description is determined by the first non-null description found in the list of
    // scalars.
    [Fact]
    public void Merge_ScalarsUsesFirstNonNullDescription_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // If no descriptions are found, the final description is null.
    [Fact]
    public void Merge_ScalarsNoDescription_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // Built-in Fusion scalar types should not be merged (FieldSelectionSet/Map).
    [Fact]
    public void Merge_BuiltInFusionScalarTypes_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                directive @provides(fields: FieldSelectionSet!) on FIELD_DEFINITION
                directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
                """
            ],
            "");
    }
}
