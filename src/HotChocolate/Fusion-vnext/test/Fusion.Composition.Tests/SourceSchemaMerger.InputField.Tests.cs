namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInputFieldTests : SourceSchemaMergerTestBase
{
    // Suppose we have two input type definitions for the same "OrderFilter" input field, defined in
    // separate schemas. In the final schema, "minTotal" is defined using the most restrictive type
    // (Int!), has a default value of 0, and includes the description from the original field in
    // Schema A.
    [Fact]
    public void Merge_InputFields_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // If any of the input fields is marked as @inaccessible, then the merged input field is also
    // marked as @inaccessible in the execution schema.
    [Fact]
    public void Merge_InaccessibleInputField_MatchesSnapshot()
    {
        AssertMatches(
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
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
                    @fusion__inaccessible
            }
            """);
    }

    // If no description is found, the merged field will have no description.
    [Fact]
    public void Merge_InputFieldsNoDescription_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // Even if an input field is only @deprecated in one source schema, the composite input field is
    // marked as @deprecated.
    [Fact]
    public void Merge_DeprecatedInputField_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                input OrderFilter {
                    minTotal: Int @deprecated(reason: "Some reason")
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
                    @deprecated(reason: "Some reason")
            }
            """);
    }

    // If the same input field is @deprecated in multiple source schemas, the first non-null
    // deprecation reason is chosen.
    [Fact]
    public void Merge_DeprecatedInputFieldsUsesFirstNonNullReason_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                input OrderFilter {
                    minTotal: Int @deprecated(reason: "Some reason")
                }
                """,
                """
                # Schema B
                input OrderFilter {
                    minTotal: Int @deprecated(reason: "Another reason")
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
                    @deprecated(reason: "Some reason")
            }
            """);
    }

    // If an input field is deprecated without a deprecation reason, a default reason is inserted to
    // be compatible with the latest spec.
    [Fact]
    public void Merge_DeprecatedInputFieldsWithoutReasonInsertsDefaultReason_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                input OrderFilter {
                    minTotal: Int @deprecated
                }
                """,
                """
                # Schema B
                input OrderFilter {
                    minTotal: Int @deprecated
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
                    @deprecated(reason: "No longer supported.")
            }
            """);
    }
}
