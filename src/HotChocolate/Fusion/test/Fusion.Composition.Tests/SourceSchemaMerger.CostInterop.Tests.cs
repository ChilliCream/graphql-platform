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
    // Schema B serves the field without any @listSize usage at all, and no default list size is
    // configured (unbounded), so the sound bound is unbounded and the public directive omits
    // assumedSize (R-COMPOSITION-WEIGHT-FOLD) while the provenance entry for A is unaffected.
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
                @listSize
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, assumedSize: 5)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // R-COMPOSITION-WEIGHT-FOLD / hc-3-oye.6 F1: a member that only serves a list field through
    // @external (reachable via a @provides path on another source) still counts as a serving
    // source with no @listSize of its own. Conservative/sound reading: since the external source
    // returns its own unannotated list data, the field's bound is unbounded unless a
    // DefaultListSize is configured, exactly like an unannotated non-external serving source.
    // The @fusion__listSize provenance entry for the annotated source (A) is unaffected, and the
    // partial-member marker (@fusion__field(schema: B, partial: true)) is retained.
    [Fact]
    public void Merge_ListSizeDirective_ExternalPartialServingSource_NoDefaultOmitsAssumedSize_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A (entity source, declares tags with assumedSize)
                type Query {
                    productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                    id: ID!
                    tags: [String] @listSize(assumedSize: 5)
                }
                """,
                """
                # Schema B (serves tags via @provides; tags itself is only @external there)
                type Query {
                    reviews: [Review!]
                }

                type Review {
                    id: ID!
                    product: Product @provides(fields: "tags")
                }

                type Product @key(fields: "id") {
                    id: ID!
                    tags: [String] @external
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              reviews: [Review!] @fusion__field(schema: B)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "productById(id: ID!): Product"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              tags: [String]
                @listSize
                @fusion__field(schema: A)
                @fusion__field(schema: B, partial: true)
                @fusion__listSize(schema: A, assumedSize: 5)
            }

            type Review @fusion__type(schema: B) {
              id: ID! @fusion__field(schema: B)
              product: Product @fusion__field(schema: B, provides: "tags")
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // Same schemas as above, but with a configured DefaultListSize: the sound bound becomes
    // max(declared assumedSize, DefaultListSize), so the public @listSize now carries the
    // (higher) default, while the @fusion__listSize provenance for A stays untouched.
    [Fact]
    public void Merge_ListSizeDirective_ExternalPartialServingSource_DefaultListSizeApplies_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A (entity source, declares tags with assumedSize)
                type Query {
                    productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                    id: ID!
                    tags: [String] @listSize(assumedSize: 5)
                }
                """,
                """
                # Schema B (serves tags via @provides; tags itself is only @external there)
                type Query {
                    reviews: [Review!]
                }

                type Review {
                    id: ID!
                    product: Product @provides(fields: "tags")
                }

                type Product @key(fields: "id") {
                    id: ID!
                    tags: [String] @external
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              reviews: [Review!] @fusion__field(schema: B)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "productById(id: ID!): Product"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              tags: [String]
                @listSize(assumedSize: 10)
                @fusion__field(schema: A)
                @fusion__field(schema: B, partial: true)
                @fusion__listSize(schema: A, assumedSize: 5)
            }

            type Review @fusion__type(schema: B) {
              id: ID! @fusion__field(schema: B)
              product: Product @fusion__field(schema: B, provides: "tags")
            }
            """,
            configure: options => options.DefaultListSize = 10,
            modifySchema: s_removeListSizeDirective);
    }

    private static readonly ListSizeMutableDirectiveDefinition s_listSizeDirective
        = new(BuiltIns.Int.Create(), BuiltIns.String.Create(), BuiltIns.Boolean.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeListSizeDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.ListSize);

    private static readonly Action<MutableSchemaDefinition> s_removeCostDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.Cost);
}
