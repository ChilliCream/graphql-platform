namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class OptInFeatureStabilityMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new OptInFeatureStabilityMismatchRule();

    // Both schemas declare the same stability for the "experimental" feature, so no error is
    // logged.
    [Fact]
    public void Validate_SameStability_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            schema @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
                query: Query
            }

            type Query { field: String }
            """,
            """
            # Schema B
            schema @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
                query: Query
            }

            type Query { field: String }
            """
        ]);
    }

    // Two schemas declare different stabilities for the same feature, which must produce a
    // composition error.
    [Fact]
    public void Validate_DifferentStability_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                schema @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
                    query: Query
                }

                type Query { field: String }
                """,
                """
                # Schema B
                schema @optInFeatureStability(feature: "experimental", stability: "PREVIEW") {
                    query: Query
                }

                type Query { field: String }
                """
            ],
            [
                """
                {
                    "message": "The opt-in feature 'experimental' has a different stability in schema 'A' (EXPERIMENTAL) than it does in schema 'B' (PREVIEW).",
                    "code": "OPT_IN_FEATURE_STABILITY_MISMATCH",
                    "severity": "Error",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
