using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerDirectiveTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string[] sdl, TagMergeBehavior tagMergeBehavior, string executionSchema)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions
            {
                AddFusionDefinitions = false,
                RemoveUnreferencedTypes = false,
                TagMergeBehavior = tagMergeBehavior
            });

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }

    public static TheoryData<string[], TagMergeBehavior, string> ExamplesData()
    {
        return new TheoryData<string[], TagMergeBehavior, string>
        {
            // Ignore @tag directives.
            {
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
                TagMergeBehavior.Ignore,
                """
                scalar Foo
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                """
            },
            // Merge @tag directives when the definitions match the canonical definition.
            {
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
                TagMergeBehavior.Include,
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
                """
            },
            // Do not merge @tag directives when the definitions do not match the canonical definition.
            {
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
                TagMergeBehavior.Include,
                """
                scalar Foo
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                """
            },
            // Merge @tag directives privately.
            {
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
                TagMergeBehavior.IncludePrivate,
                $"""
                scalar Foo
                    @fusion__tag(name: "a")
                    @fusion__tag(name: "b")
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)

                directive @fusion__tag(name: String!) repeatable on {s_tagLocations}
                """
            },
            // Merge @tag directives with the same name.
            {
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
                TagMergeBehavior.Include,
                $"""
                scalar Foo
                    @tag(name: "same")
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)

                directive @tag(name: String!) repeatable on {s_tagLocations}
                """
            }
        };
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
