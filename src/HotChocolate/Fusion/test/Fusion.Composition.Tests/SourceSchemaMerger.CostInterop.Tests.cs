using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerCostInteropTests : SourceSchemaMergerTestBase
{
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

    [Fact]
    public void Merge_ListSizeDirective_ExternalPartialMember_NotAServingSource_MatchesSnapshot()
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
                @listSize(assumedSize: 5)
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

    [Fact]
    public void Merge_ListSizeDirective_ExternalPartialMember_DefaultListSizeDoesNotApply_MatchesSnapshot()
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
                @listSize(assumedSize: 5)
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

    [Fact]
    public void Merge_ListSizeDirective_ExternalPartialMember_OwnDeclaredAssumedSizeFoldsIn_MatchesSnapshot()
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
                # Schema B (serves tags via @provides; tags itself is only @external there, but
                # declares its own, larger assumedSize)
                type Query {
                    reviews: [Review!]
                }

                type Review {
                    id: ID!
                    product: Product @provides(fields: "tags")
                }

                type Product @key(fields: "id") {
                    id: ID!
                    tags: [String] @external @listSize(assumedSize: 8)
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
                @listSize(assumedSize: 8)
                @fusion__field(schema: A)
                @fusion__field(schema: B, partial: true)
                @fusion__listSize(schema: A, assumedSize: 5)
                @fusion__listSize(schema: B, assumedSize: 8)
            }

            type Review @fusion__type(schema: B) {
              id: ID! @fusion__field(schema: B)
              product: Product @fusion__field(schema: B, provides: "tags")
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
