using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerRequiresOptInTests : SourceSchemaMergerTestBase
{
    // A field annotated with @requiresOptIn in both schemas carries the union of all unique
    // features in the execution schema: one directive per distinct feature.
    [Fact]
    public void Merge_RequiresOptIn_UnionsFeatures()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @requiresOptIn(feature: "alpha")
                }

                {{s_requiresOptInDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: String @requiresOptIn(feature: "alpha") @requiresOptIn(feature: "beta")
                }

                {{s_requiresOptInDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: String
                @requiresOptIn(feature: "alpha")
                @requiresOptIn(feature: "beta")
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeRequiresOptInDirective);
    }

    // A field annotated with @requiresOptIn in only one source schema still requires opt-in in
    // the composite execution schema: opt-in in any source means opt-in in the composite.
    [Fact]
    public void Merge_RequiresOptIn_DisjointSchemas_FieldRemainsOptIn()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @requiresOptIn(feature: "alpha")
                }

                {{s_requiresOptInDirective}}
                """,
                """
                # Schema B
                type Query {
                    field: String
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: String
                @requiresOptIn(feature: "alpha")
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeRequiresOptInDirective);
    }

    private static readonly RequiresOptInMutableDirectiveDefinition s_requiresOptInDirective
        = new(BuiltIns.String.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeRequiresOptInDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.RequiresOptIn);
}
