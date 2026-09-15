using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerListSizeDirectiveTests : SourceSchemaMergerTestBase
{
    [Fact]
    public void DefaultListSize_Should_Throw_When_Negative()
    {
        // arrange
        var options = new SourceSchemaMergerOptions();

        // act
        void Act() => options.DefaultListSize = -1;

        // assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(Act);
        Assert.Equal(nameof(SourceSchemaMergerOptions.DefaultListSize), exception.ParamName);
    }

    [Fact]
    public void Compose_Should_Fail_When_AssumedSizeIsNegative()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    $$"""
                    type Query {
                        field: [Int] @listSize(assumedSize: -1)
                    }

                    {{s_listSizeDirective}}
                    """)
            ],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The argument 'assumedSize' of the @listSize directive on field 'Query.field' in schema 'A' must not be negative (-1).",
                "code": "INVALID_GRAPHQL",
                "severity": "Error",
                "coordinate": "Query.field",
                "member": "field",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_SlicingArgumentDefaultValueIsNegative()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    $$"""
                    type Query {
                        field: [Int] @listSize(slicingArgumentDefaultValue: -2)
                    }

                    {{s_listSizeDirective}}
                    """)
            ],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The argument 'slicingArgumentDefaultValue' of the @listSize directive on field 'Query.field' in schema 'A' must not be negative (-2).",
                "code": "INVALID_GRAPHQL",
                "severity": "Error",
                "coordinate": "Query.field",
                "member": "field",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_SlicingArgumentsIsBareInt()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    $$"""
                    type Query {
                        field: [Int] @listSize(slicingArguments: 1)
                    }

                    {{s_listSizeDirective}}
                    """)
            ],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The argument 'slicingArguments' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value (1).",
                "code": "INVALID_GRAPHQL",
                "severity": "Error",
                "coordinate": "Query.field",
                "member": "field",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_SlicingArgumentsListHasIntItem()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    $$"""
                    type Query {
                        field: [Int] @listSize(slicingArguments: ["first", 1])
                    }

                    {{s_listSizeDirective}}
                    """)
            ],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The argument 'slicingArguments' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value ([\"first\", 1]).",
                "code": "INVALID_GRAPHQL",
                "severity": "Error",
                "coordinate": "Query.field",
                "member": "field",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Merge_Should_PreserveZero_When_ListSizeArgumentsAreZero()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: [Int]
                        @listSize(assumedSize: 0, slicingArgumentDefaultValue: 0)
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: [Int]
                @listSize(assumedSize: 0, slicingArgumentDefaultValue: 0)
                @fusion__field(schema: A)
                @fusion__listSize(schema: A, assumedSize: 0, slicingArgumentDefaultValue: 0)
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: [Int] @fusion__field(schema: A) @fusion__field(schema: B)
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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field1: [Int]
                @listSize(slicingArguments: ["first", "last"])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A)
                @fusion__listSize(schema: B, slicingArguments: ["first", "last"])
              field2: [Int]
                @listSize(slicingArguments: ["first", "last"])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, slicingArguments: null)
                @fusion__listSize(schema: B, slicingArguments: ["first", "last"])
              field3: [Int]
                @listSize(slicingArguments: ["first", "last", "another"])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, slicingArguments: ["first", "last"])
                @fusion__listSize(schema: B, slicingArguments: ["first", "last", "another"])
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // A single value in a list position is coerced to a one-element list (GraphQL list input
    // coercion), so a singleton string is a valid shorthand for slicingArguments. The public
    // directive and the @fusion__listSize provenance entry agree: both report the coerced list.
    [Fact]
    public void Merge_ListSizeDirectiveSlicingArgumentsSingleton_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: [Int] @listSize(slicingArguments: "first")
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: [Int]
                @listSize(slicingArguments: ["first"])
                @fusion__field(schema: A)
                @fusion__listSize(schema: A, slicingArguments: ["first"])
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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field1: [Int]
                @listSize(sizedFields: ["edges", "nodes"])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A)
                @fusion__listSize(schema: B, sizedFields: ["edges", "nodes"])
              field2: [Int]
                @listSize(sizedFields: ["edges", "nodes"])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, sizedFields: null)
                @fusion__listSize(schema: B, sizedFields: ["edges", "nodes"])
              field3: [Int]
                @listSize(sizedFields: ["edges", "nodes", "another"])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, sizedFields: ["edges", "nodes"])
                @fusion__listSize(schema: B, sizedFields: ["edges", "nodes", "another"])
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // A single value in a list position is coerced to a one-element list (GraphQL list input
    // coercion), so a singleton string is a valid shorthand for sizedFields. The public directive
    // and the @fusion__listSize provenance entry agree: both report the coerced list.
    [Fact]
    public void Merge_ListSizeDirectiveSizedFieldsSingleton_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: [Int] @listSize(sizedFields: "edges")
                }

                {{s_listSizeDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: [Int]
                @listSize(sizedFields: ["edges"])
                @fusion__field(schema: A)
                @fusion__listSize(schema: A, sizedFields: ["edges"])
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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
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

    // R-REQUIRE-ONE-DEFAULT: an omitted requireOneSlicingArgument usage contributes the source
    // definition's own declared default (true here), which then folds true-if-any against a
    // source that explicitly opts out.
    [Fact]
    public void Merge_ListSizeDirectiveRequireOneSlicingArgument_SourceDefaultAppliesBeforeFold_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }

                directive @listSize(
                    assumedSize: Int
                    slicingArguments: [String!]
                    sizedFields: [String!]
                    requireOneSlicingArgument: Boolean = true
                    slicingArgumentDefaultValue: Int
                ) on FIELD_DEFINITION
                """,
                $$"""
                # Schema B
                type Query {
                    field: [Int] @listSize(assumedSize: 5, requireOneSlicingArgument: false)
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
                @listSize(assumedSize: 5, requireOneSlicingArgument: true)
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, assumedSize: 5)
                @fusion__listSize(
                  schema: B
                  assumedSize: 5
                  requireOneSlicingArgument: false
                )
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // hc-3-mmh.9 / R-COMPOSITION-WEIGHT-FOLD: when a field is served by an annotated source and
    // an unannotated one, the composite bound must also cover the unannotated source's effective
    // size, which composition only knows through the configured DefaultListSize
    // (@fusion__cost_options(defaultListSize:)). The sound bound is the greater of the two.
    [Fact]
    public void Merge_ListSizeDirective_UnannotatedServingSource_DefaultListSizeRaisesFloor_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A (declares assumedSize: 5)
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }
                """,
                """
                # Schema B (serves the field, no @listSize at all)
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
                @listSize(assumedSize: 10)
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, assumedSize: 5)
            }
            """,
            configure: options => options.DefaultListSize = 10,
            modifySchema: s_removeListSizeDirective);
    }

    // The default is only a floor: a higher declared assumedSize still wins.
    [Fact]
    public void Merge_ListSizeDirective_UnannotatedServingSource_DeclaredWinsOverLowerDefault_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A (declares assumedSize: 5)
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }
                """,
                """
                # Schema B (serves the field, no @listSize at all)
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
            configure: options => options.DefaultListSize = 3,
            modifySchema: s_removeListSizeDirective);
    }

    // When DefaultListSize is unset (unbounded), the unannotated source's effective size is
    // unknown, so assumedSize is omitted entirely rather than reporting the lower, unsound,
    // declared-only value. Other folded arguments (here slicingArguments) are unaffected.
    [Fact]
    public void Merge_ListSizeDirective_UnannotatedServingSource_NoDefaultOmitsAssumedSize_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A (declares assumedSize and slicingArguments)
                type Query {
                    field: [Int] @listSize(assumedSize: 5, slicingArguments: ["first"])
                }

                {{s_listSizeDirective}}
                """,
                """
                # Schema B (serves the field, no @listSize at all)
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
                @listSize(slicingArguments: ["first"])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__listSize(schema: A, assumedSize: 5, slicingArguments: ["first"])
            }
            """,
            modifySchema: s_removeListSizeDirective);
    }

    // When every serving source declares a compatible @listSize, the fold is unaffected by
    // DefaultListSize even when one is configured.
    [Fact]
    public void Merge_ListSizeDirective_AllSourcesAnnotated_DefaultListSizeDoesNotApply_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
                }
                """,
                """
                # Schema B
                type Query {
                    field: [Int] @listSize(assumedSize: 5)
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
                @fusion__listSize(schema: B, assumedSize: 5)
            }
            """,
            configure: options => options.DefaultListSize = 3,
            modifySchema: s_removeListSizeDirective);
    }

    private static readonly ListSizeMutableDirectiveDefinition s_listSizeDirective
        = new(BuiltIns.Int.Create(), BuiltIns.String.Create(), BuiltIns.Boolean.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeListSizeDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.ListSize);
}
