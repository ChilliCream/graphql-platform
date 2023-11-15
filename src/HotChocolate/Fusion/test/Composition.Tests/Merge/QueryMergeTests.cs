using CookieCrumble;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class QueryMergeTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Identical_Query_Fields_Merge()
        => await Succeed(
                """
                type Query {
                  field1: String!
                }
                """,
                """
                type Query {
                  field1: String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Query {
                  field1: String!
                    @resolver(operation: "{ field1 }", kind: FETCH, subgraph: "A")
                    @resolver(operation: "{ field1 }", kind: FETCH, subgraph: "B")
                }
                """);


    [Fact]
    public async Task Identical_Query_Fields_With_Arguments_Merge()
        => await Succeed(
                """
                type Query {
                  field1(a: String): String!
                }
                """,
                """
                type Query {
                  field1(a: String): String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Query {
                  field1(a: String): String!
                    @resolver(operation: "query($a: String) { field1(a: $a) }", kind: FETCH, subgraph: "A")
                    @resolver(operation: "query($a: String) { field1(a: $a) }", kind: FETCH, subgraph: "B")
                }
                """);

    [Fact]
    public async Task Identical_Query_Fields_With_Arguments_Use_Original_Name_Merge()
        => await Succeed(
                """
                type Query {
                  field1(a: Foo): Foo!
                }

                scalar Foo
                """,
                """
                type Query {
                  field1(a: Bar): Bar!
                }

                scalar Bar

                schema @rename(coordinate: "Bar", newName: "Foo") {
                  query: Query
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Query {
                  field1(a: Foo): Foo!
                    @resolver(operation: "query($a: Foo) { field1(a: $a) }", kind: FETCH, subgraph: "A")
                    @resolver(operation: "query($a: Bar) { field1(a: $a) }", kind: FETCH, subgraph: "B")
                }

                scalar Foo
                  @source(subgraph: "A")
                  @source(subgraph: "B", name: "Bar")
                """);

    [Fact]
    public async Task Identical_Query_Fields_And_Object_Merge()
        => await Succeed(
                """
                type Query {
                  foo: Foo!
                }

                type Foo {
                  field1: String!
                }
                """,
                """
                type Query {
                  foo: Foo!
                }

                type Foo {
                  field2: String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Foo
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                  field2: String!
                    @source(subgraph: "B")
                }

                type Query {
                  foo: Foo!
                    @resolver(operation: "{ foo }", kind: FETCH, subgraph: "A")
                    @resolver(operation: "{ foo }", kind: FETCH, subgraph: "B")
                }
                """);
}