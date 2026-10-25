using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerOptInFeatureStabilityTests : SourceSchemaMergerTestBase
{
    // When both schemas declare the same stability for the same feature, the execution schema
    // carries exactly one @optInFeatureStability directive per feature.
    [Fact]
    public void Merge_OptInFeatureStability_DedupesByFeature()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                schema @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
                    query: Query
                }

                type Query { field: String }

                {{s_optInFeatureStabilityDirective}}
                """,
                $$"""
                # Schema B
                schema @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
                    query: Query
                }

                type Query { field: String }

                {{s_optInFeatureStabilityDirective}}
                """
            ],
            """
            schema
              @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: String @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeOptInFeatureStabilityDirective);
    }

    private static readonly OptInFeatureStabilityMutableDirectiveDefinition s_optInFeatureStabilityDirective
        = new(BuiltIns.String.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeOptInFeatureStabilityDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.OptInFeatureStability);
}
