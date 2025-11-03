using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerTagDirectiveTests : SourceSchemaMergerTestBase
{
    // Ignore @tag directives.
    [Fact]
    public void Merge_TagDirectivesIgnore_MatchesSnapshot()
    {
        AssertMatches(
            [
                $"""
                # Schema A
                scalar Foo @tag(name: "a")

                directive @tag(name: String!) repeatable on {s_tagLocations}
                """,
                $"""
                # Schema B
                scalar Foo @tag(name: "b")

                directive @tag(name: String!) repeatable on {s_tagLocations}
                """
            ],
            """
            scalar Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B)
            """,
            options => options.TagMergeBehavior = DirectiveMergeBehavior.Ignore);
    }

    // Merge @tag directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_TagDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                schema @tag(name: "a") {
                    query: Query
                }

                type Query {
                    field: Int
                }

                type FooObject @tag(name: "a") {
                    field(arg: Int @tag(name: "a")): Int @tag(name: "a")
                }

                interface FooInterface @tag(name: "a") {
                    field(arg: Int @tag(name: "a")): Int @tag(name: "a")
                }

                union FooUnion @tag(name: "a") = FooObject

                scalar FooScalar @tag(name: "a")

                enum FooEnum @tag(name: "a") {
                    VALUE @tag(name: "a")
                }

                input FooInput @tag(name: "a") {
                    field: Int @tag(name: "a")
                }

                directive @tag(name: String!) repeatable on {{s_tagLocations}}
                """,
                $$"""
                # Schema B
                schema @tag(name: "b") {
                    query: Query
                }

                type Query {
                    field: Int
                }

                type FooObject @tag(name: "b") {
                    field(arg: Int @tag(name: "b")): Int @tag(name: "b")
                }

                interface FooInterface @tag(name: "b") {
                    field(arg: Int @tag(name: "b")): Int @tag(name: "b")
                }

                union FooUnion @tag(name: "b") = FooObject

                scalar FooScalar @tag(name: "b")

                enum FooEnum @tag(name: "b") {
                    VALUE @tag(name: "b")
                }

                input FooInput @tag(name: "b") {
                    field: Int @tag(name: "b")
                }

                directive @tag(name: String!) repeatable on {{s_tagLocations}}
                """
            ],
            $$"""
            schema
                @tag(name: "a")
                @tag(name: "b") {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            type FooObject
                @tag(name: "a")
                @tag(name: "b")
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field(arg: Int
                    @tag(name: "a")
                    @tag(name: "b")
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)): Int
                    @tag(name: "a")
                    @tag(name: "b")
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            interface FooInterface
                @tag(name: "a")
                @tag(name: "b")
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field(arg: Int
                    @tag(name: "a")
                    @tag(name: "b")
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)): Int
                    @tag(name: "a")
                    @tag(name: "b")
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            union FooUnion
                @tag(name: "a")
                @tag(name: "b")
                @fusion__type(schema: A)
                @fusion__type(schema: B)
                @fusion__unionMember(schema: A, member: "FooObject")
                @fusion__unionMember(schema: B, member: "FooObject") = FooObject

            input FooInput
                @tag(name: "a")
                @tag(name: "b")
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @tag(name: "a")
                    @tag(name: "b")
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
            }

            enum FooEnum
                @tag(name: "a")
                @tag(name: "b")
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                VALUE
                    @tag(name: "a")
                    @tag(name: "b")
                    @fusion__enumValue(schema: A)
                    @fusion__enumValue(schema: B)
            }

            scalar FooScalar
                @tag(name: "a")
                @tag(name: "b")
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            directive @tag(name: String!) repeatable on {{s_tagLocations}}
            """,
            options => options.TagMergeBehavior = DirectiveMergeBehavior.Include);
    }

    // Do not merge @tag directives when the definitions do not match the canonical definition.
    [Fact]
    public void Merge_TagDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                scalar Foo @tag(name: "a")

                directive @tag(name: String) repeatable on SCALAR
                """,
                """
                # Schema B
                scalar Foo @tag(name: "b")

                directive @tag(id: Int) repeatable on SCALAR
                """
            ],
            """
            scalar Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B)
            """,
            options => options.TagMergeBehavior = DirectiveMergeBehavior.Include);
    }

    // Merge @tag directives privately.
    [Fact]
    public void Merge_TagDirectivesPrivately_MatchesSnapshot()
    {
        AssertMatches(
            [
                $"""
                # Schema A
                scalar Foo @tag(name: "a")

                directive @tag(name: String!) repeatable on {s_tagLocations}
                """,
                $"""
                # Schema B
                scalar Foo @tag(name: "b")

                directive @tag(name: String!) repeatable on {s_tagLocations}
                """
            ],
            $"""
            scalar Foo
                @fusion__tag(name: "a")
                @fusion__tag(name: "b")
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            directive @fusion__tag(name: String!) repeatable on {s_tagLocations}
            """,
            options => options.TagMergeBehavior = DirectiveMergeBehavior.IncludePrivate);
    }

    // Merge @tag directives with the same name.
    [Fact]
    public void Merge_TagDirectivesSameName_MatchesSnapshot()
    {
        AssertMatches(
            [
                $"""
                # Schema A
                scalar Foo @tag(name: "same") @tag(name: "same")

                directive @tag(name: String!) repeatable on {s_tagLocations}
                """,
                $"""
                # Schema B
                scalar Foo @tag(name: "same") @tag(name: "same")

                directive @tag(name: String!) repeatable on {s_tagLocations}
                """
            ],
            $"""
            scalar Foo
                @tag(name: "same")
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            directive @tag(name: String!) repeatable on {s_tagLocations}
            """,
            options => options.TagMergeBehavior = DirectiveMergeBehavior.Include);
    }

    private static readonly string s_tagLocations =
        """
        SCHEMA
        | SCALAR
        | OBJECT
        | FIELD_DEFINITION
        | ARGUMENT_DEFINITION
        | INTERFACE
        | UNION
        | ENUM
        | ENUM_VALUE
        | INPUT_OBJECT
        | INPUT_FIELD_DEFINITION
        """.ReplaceLineEndings(" ");
}
