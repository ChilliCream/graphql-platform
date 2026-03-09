namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class SpecifiedByUrlMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new SpecifiedByUrlMismatchRule();

    // All schemas have the same specified-by URL for the "Date" scalar type.
    [Fact]
    public void Validate_SpecifiedByUrlMatch_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            scalar Date @specifiedBy(url: "https://example.com/date-spec")
            """,
            """
            # Schema B
            scalar Date @specifiedBy(url: "https://example.com/date-spec")
            """
        ]);
    }

    // The schemas have different specified-by URLs for the "Date" scalar type, which should trigger
    // warnings.
    [Fact]
    public void Validate_SpecifiedByUrlMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                scalar Date @specifiedBy(url: "https://example.com/date-spec-1")
                """,
                """
                # Schema B
                scalar Date @specifiedBy(url: "https://example.com/date-spec-2")
                """,
                """
                # Schema C
                scalar Date
                """
            ],
            [
                """
                {
                    "message": "The scalar type 'Date' has a different specified-by URL in schema 'A' (https://example.com/date-spec-1) than it does in schema 'B' (https://example.com/date-spec-2).",
                    "code": "SPECIFIED_BY_URL_MISMATCH",
                    "severity": "Warning",
                    "coordinate": "Date",
                    "member": "Date",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The scalar type 'Date' has a different specified-by URL in schema 'B' (https://example.com/date-spec-2) than it does in schema 'C' (null).",
                    "code": "SPECIFIED_BY_URL_MISMATCH",
                    "severity": "Warning",
                    "coordinate": "Date",
                    "member": "Date",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
