namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerClientDirectiveTests : SourceSchemaMergerTestBase
{
    [Fact]
    public void Merge_Should_KeepClientDirective_When_EverySourceDefinesIt()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker(note: String!) on FIELD
                """,
                """
                type Product {
                  b: String
                }

                directive @marker(note: String!) on FIELD
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }

            directive @marker(note: String!) on FIELD
            """);
    }

    [Fact]
    public void Merge_Should_DropClientDirective_When_ASourceDoesNotDefineIt()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker(note: String!) on FIELD
                """,
                """
                type Product {
                  b: String
                }
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_KeepSharedForwardedLocations_When_LocationsDiffer()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker on FIELD | VARIABLE_DEFINITION | INLINE_FRAGMENT | QUERY
                """,
                """
                type Product {
                  b: String
                }

                directive @marker on FIELD | INLINE_FRAGMENT | QUERY | FIELD_DEFINITION
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }

            directive @marker on FIELD
            """);
    }

    [Fact]
    public void Merge_Should_DropClientDirective_When_NoForwardedLocationIsShared()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker on FIELD | INLINE_FRAGMENT
                """,
                """
                type Product {
                  b: String
                }

                directive @marker on VARIABLE_DEFINITION | INLINE_FRAGMENT
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_MarkClientDirectiveRepeatable_When_EverySourceMarksItRepeatable()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker repeatable on FIELD

                directive @label repeatable on FIELD
                """,
                """
                type Product {
                  b: String
                }

                directive @marker repeatable on FIELD

                directive @label on FIELD
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }

            directive @label on FIELD

            directive @marker repeatable on FIELD
            """);
    }

    [Fact]
    public void Merge_Should_KeepSharedArguments_When_ArgumentsDiffer()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker(
                  note: String
                  level: Int = 1
                  priority: Int = 1
                  onlyInA: Boolean
                ) on FIELD
                """,
                """
                type Product {
                  b: String
                }

                directive @marker(
                  note: String!
                  level: Int = 1
                  priority: Int = 2
                  onlyInB: String = "b"
                ) on FIELD
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }

            directive @marker(level: Int = 1, note: String!, priority: Int) on FIELD
            """);
    }

    [Fact]
    public void Merge_Should_DropClientDirective_When_RequiredArgumentIsNotDefinedInEverySource()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker(note: String!) on FIELD
                """,
                """
                type Product {
                  b: String
                }

                directive @marker on FIELD
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_DropClientDirective_When_ArgumentTypesCannotBeMerged()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                directive @marker(note: String) on FIELD
                """,
                """
                type Product {
                  b: String
                }

                directive @marker(note: [String]) on FIELD
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_DropArgument_When_ArgumentTypeIsInaccessible()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: String
                }

                enum Level @inaccessible {
                  LOW
                  HIGH
                }

                directive @marker(level: Level, note: String) on FIELD
                """,
                """
                type Product {
                  b: String
                }

                enum Level {
                  LOW
                  HIGH
                }

                directive @marker(level: Level, note: String) on FIELD
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: String @fusion__field(schema: A)
              b: String @fusion__field(schema: B)
            }

            enum Level
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__inaccessible {
              LOW @fusion__enumValue(schema: A) @fusion__enumValue(schema: B)
              HIGH @fusion__enumValue(schema: A) @fusion__enumValue(schema: B)
            }

            directive @marker(note: String) on FIELD
            """);
    }

    [Fact]
    public void Merge_Should_NotMergeGatewayDirectives_When_EverySourceDefinesThem()
    {
        AssertMatches(
            [
                """
                type Product {
                  a: [String]
                }

                directive @defer(if: Boolean! = true, label: String) on FRAGMENT_SPREAD | INLINE_FRAGMENT

                directive @stream(if: Boolean! = true, initialCount: Int! = 0, label: String) on FIELD
                """,
                """
                type Product {
                  b: [String]
                }

                directive @defer(if: Boolean! = true, label: String) on FRAGMENT_SPREAD | INLINE_FRAGMENT

                directive @stream(if: Boolean! = true, initialCount: Int! = 0, label: String) on FIELD
                """
            ],
            """
            type Product @fusion__type(schema: A) @fusion__type(schema: B) {
              a: [String] @fusion__field(schema: A)
              b: [String] @fusion__field(schema: B)
            }
            """);
    }
}
