using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerListSizeDirectiveTests : SourceSchemaMergerTestBase
{
    // Merge @listSize directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_ListSizeDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }

                {{s_listSizeDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: [Int]
                    @listSize(assumedSize: 5)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, assumedSize: 5)
                    @fusion__listSize(schema: B, assumedSize: 5)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // Do not merge @listSize directives when the definitions do not match the canonical definition.
    [Fact]
    public void Merge_ListSizeDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field: [Int] @listSize(min: 5, max: 10)
                }

                directive @listSize(min: Int!, max: Int!) repeatable on SCALAR
                """,
                """
                # Schema B
                type Query {
                    field: [Int] @listSize(limit: 5)
                }

                directive @listSize(limit: Int!) on SCALAR
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: [Int]
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // Merge the maximum assumed size.
    [Fact]
    public void Merge_ListSizeDirectivesMaxAssumedSize_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field1: [Int] @listSize
                    field2: [Int] @listSize(assumedSize: null)
                    field3: [Int] @listSize(assumedSize: 10)
                }

                {{s_listSizeDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field1: [Int] @listSize(assumedSize: 5)
                    field2: [Int] @listSize(assumedSize: 5)
                    field3: [Int] @listSize(assumedSize: 5)
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: [Int]
                    @listSize(assumedSize: 5)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A)
                    @fusion__listSize(schema: B, assumedSize: 5)
                field2: [Int]
                    @listSize(assumedSize: 5)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, assumedSize: null)
                    @fusion__listSize(schema: B, assumedSize: 5)
                field3: [Int]
                    @listSize(assumedSize: 10)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, assumedSize: 10)
                    @fusion__listSize(schema: B, assumedSize: 5)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // Merge the union of slicing arguments.
    [Fact]
    public void Merge_ListSizeDirectivesUnionSlicingArguments_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field1: [Int] @listSize
                    field2: [Int] @listSize(slicingArguments: null)
                    field3: [Int] @listSize(slicingArguments: ["first", "last"])
                }

                {{s_listSizeDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field1: [Int] @listSize(slicingArguments: ["first", "last"])
                    field2: [Int] @listSize(slicingArguments: ["first", "last"])
                    field3: [Int] @listSize(slicingArguments: ["first", "last", "another"])
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: [Int]
                    @listSize(slicingArguments: [ "first", "last" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A)
                    @fusion__listSize(schema: B, slicingArguments: [ "first", "last" ])
                field2: [Int]
                    @listSize(slicingArguments: [ "first", "last" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, slicingArguments: null)
                    @fusion__listSize(schema: B, slicingArguments: [ "first", "last" ])
                field3: [Int]
                    @listSize(slicingArguments: [ "first", "last", "another" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, slicingArguments: [ "first", "last" ])
                    @fusion__listSize(schema: B, slicingArguments: [ "first", "last", "another" ])
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // Merge the union of sized fields.
    [Fact]
    public void Merge_ListSizeDirectivesUnionSizedFields_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field1: [Int] @listSize
                    field2: [Int] @listSize(sizedFields: null)
                    field3: [Int] @listSize(sizedFields: ["edges", "nodes"])
                }

                {{s_listSizeDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field1: [Int] @listSize(sizedFields: ["edges", "nodes"])
                    field2: [Int] @listSize(sizedFields: ["edges", "nodes"])
                    field3: [Int] @listSize(sizedFields: ["edges", "nodes", "another"])
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: [Int]
                    @listSize(sizedFields: [ "edges", "nodes" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A)
                    @fusion__listSize(schema: B, sizedFields: [ "edges", "nodes" ])
                field2: [Int]
                    @listSize(sizedFields: [ "edges", "nodes" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, sizedFields: null)
                    @fusion__listSize(schema: B, sizedFields: [ "edges", "nodes" ])
                field3: [Int]
                    @listSize(sizedFields: [ "edges", "nodes", "another" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, sizedFields: [ "edges", "nodes" ])
                    @fusion__listSize(schema: B, sizedFields: [ "edges", "nodes", "another" ])
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // Merge requireOneSlicingArgument.
    [Fact]
    public void Merge_ListSizeDirectivesRequireOneSlicingArgument_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field1: [Int] @listSize
                    field2: [Int] @listSize(requireOneSlicingArgument: null)
                    field3: [Int] @listSize(requireOneSlicingArgument: true)
                    field4: [Int] @listSize(requireOneSlicingArgument: false)
                }

                {{s_listSizeDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field1: [Int] @listSize(requireOneSlicingArgument: true)
                    field2: [Int] @listSize(requireOneSlicingArgument: true)
                    field3: [Int] @listSize(requireOneSlicingArgument: false)
                    field4: [Int] @listSize(requireOneSlicingArgument: false)
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: [Int]
                    @listSize(requireOneSlicingArgument: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A)
                    @fusion__listSize(schema: B, requireOneSlicingArgument: true)
                field2: [Int]
                    @listSize(requireOneSlicingArgument: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, requireOneSlicingArgument: null)
                    @fusion__listSize(schema: B, requireOneSlicingArgument: true)
                field3: [Int]
                    @listSize(requireOneSlicingArgument: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, requireOneSlicingArgument: true)
                    @fusion__listSize(schema: B, requireOneSlicingArgument: false)
                field4: [Int]
                    @listSize(requireOneSlicingArgument: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, requireOneSlicingArgument: false)
                    @fusion__listSize(schema: B, requireOneSlicingArgument: false)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // Merge the maximum slicingArgumentDefaultValue.
    [Fact]
    public void Merge_ListSizeDirectivesMaxSlicingArgumentDefaultValue_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field1: [Int] @listSize
                    field2: [Int] @listSize(slicingArgumentDefaultValue: null)
                    field3: [Int] @listSize(slicingArgumentDefaultValue: 10)
                }

                {{s_listSizeDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field1: [Int] @listSize(slicingArgumentDefaultValue: 5)
                    field2: [Int] @listSize(slicingArgumentDefaultValue: 5)
                    field3: [Int] @listSize(slicingArgumentDefaultValue: 5)
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: [Int]
                    @listSize(slicingArgumentDefaultValue: 5)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A)
                    @fusion__listSize(schema: B, slicingArgumentDefaultValue: 5)
                field2: [Int]
                    @listSize(slicingArgumentDefaultValue: 5)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, slicingArgumentDefaultValue: null)
                    @fusion__listSize(schema: B, slicingArgumentDefaultValue: 5)
                field3: [Int]
                    @listSize(slicingArgumentDefaultValue: 10)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__listSize(schema: A, slicingArgumentDefaultValue: 10)
                    @fusion__listSize(schema: B, slicingArgumentDefaultValue: 5)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    private static readonly ListSizeMutableDirectiveDefinition s_listSizeDirective
        = new(BuiltIns.Int.Create(), BuiltIns.String.Create(), BuiltIns.Boolean.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeListSizeDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.ListSize);
}
