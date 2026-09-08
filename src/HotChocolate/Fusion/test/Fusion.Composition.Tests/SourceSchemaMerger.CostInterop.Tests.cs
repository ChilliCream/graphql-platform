using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

// R-COMPOSITION-COMPAT: composition must work with @cost/@listSize directives that lack the
// non-spec slicingArgumentDefaultValue argument, and with sources that apply @cost/@listSize
// without declaring their own directive definition at all (hc-3-mmh.9 interop addendum).
public sealed class SourceSchemaMergerCostInteropTests : SourceSchemaMergerTestBase
{
    // A source schema whose @listSize definition omits the non-spec slicingArgumentDefaultValue
    // argument (the plain IBM-spec shape) is compatible with the canonical definition and folds
    // normally alongside a source that declares the full ChilliCream shape.
    [Fact]
    public void Merge_ListSizeDirectives_SpecOnlyDefinitionComposesWithoutError_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A (plain IBM spec shape, no slicingArgumentDefaultValue)
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }

                directive @listSize(
                    assumedSize: Int
                    slicingArguments: [String!]
                    sizedFields: [String!]
                    requireOneSlicingArgument: Boolean
                ) on FIELD_DEFINITION
                """,
                $$"""
                # Schema B (full ChilliCream shape)
                type Query {
                    field: [Int] @listSize(assumedSize: 10, slicingArgumentDefaultValue: 20)
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: [Int]
                @listSize(assumedSize: 10, slicingArgumentDefaultValue: 20)
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, assumedSize: 5)
                @fusion__listSize(
                  schema: B
                  assumedSize: 10
                  slicingArgumentDefaultValue: 20
                )
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // A source schema that applies @cost without declaring its own directive definition
    // (relying on it being ambient like a spec directive) composes without error: the canonical
    // definition is injected and the usage folds normally.
    [Fact]
    public void Merge_CostDirective_UndeclaredUsageComposesWithoutError_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A (uses @cost without declaring it)
                type Query {
                    field: Int @cost(weight: "2.0")
                }
                """,
                """
                # Schema B (does not use @cost)
                type Query {
                    field: Int
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @cost(weight: "2")
                @fusion__cost(schema: A, weight: "2.0")
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeCostDirective);
    }

    // A source schema that applies @listSize without declaring its own directive definition
    // composes without error: the canonical definition is injected and the usage folds normally.
    [Fact]
    public void Merge_ListSizeDirective_UndeclaredUsageComposesWithoutError_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A (uses @listSize without declaring it)
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }
                """,
                """
                # Schema B (does not use @listSize)
                type Query {
                    field: [Int]
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: [Int]
                @listSize(assumedSize: 5)
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, assumedSize: 5)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    private static readonly ListSizeMutableDirectiveDefinition s_listSizeDirective
        = new(BuiltIns.Int.Create(), BuiltIns.String.Create(), BuiltIns.Boolean.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeListSizeDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.ListSize);

    private static readonly Action<MutableSchemaDefinition> s_removeCostDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.Cost);
}
