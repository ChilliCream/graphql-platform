using CookieCrumble;
using HotChocolate.Fusion.Composition.Pipeline;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class InterfaceMergeTests(ITestOutputHelper output)
    : CompositionTestBase(output, new InterfaceTypeMergeHandler(), new ScalarTypeMergeHandler())
{
    [Fact]
    public async Task Identical_Interfaces_Merge()
        => await Succeed(
                """
                interface Interface1 {
                  field1: String!
                }
                """,
                """
                interface Interface1 {
                  field1: String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Interfaces_With_Different_Fields_Merge()
        => await Succeed(
                """
                interface Interface1 {
                  field1: String!
                }
                """,
                """
                interface Interface1 {
                  field2: Int!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                  field2: Int!
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Fields_Merge_When_Nullability_Is_Different()
        => await Succeed(
                """
                interface Interface1 {
                  field1: String!
                }
                """,
                """
                interface Interface1 {
                  field1: String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1: String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Fields_Do_Not_Merge_When_Return_Type_Differs()
        => await Fail(
            """
            interface Interface1 {
              field1: String
            }
            """,
            """
            interface Interface1 {
              field1: Int
            }
            """,
            "F0002");

    [Fact]
    public async Task Types_With_The_Same_Name_Must_Be_Of_The_Same_Kind()
        => await Fail(
            """
            interface Interface1 {
              field1: Enum1!
            }

            enum Enum1 {
              BAR
            }
            """,
            """
            interface Interface1 {
              field1: Enum1!
            }

            scalar Enum1
            """,
            "F0002");

    [Fact]
    public async Task Fields_Merge_When_Arguments_Are_Identical()
        => await Succeed(
                """
                interface Interface1 {
                  field1(a: String): String
                }
                """,
                """
                interface Interface1 {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1(a: String): String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Fields_Merge_When_Arguments_Nullability_Is_Different()
        => await Succeed(
                """
                interface Interface1 {
                  field1(a: String!): String
                }
                """,
                """
                interface Interface1 {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1(a: String!): String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Fields_Do_Not_Merge_When_Argument_Types_Are_Different()
        => await Fail(
            """
            interface Interface1 {
              field1(a: String): String
            }
            """,
            """
            interface Interface1 {
              field1(a: Int): String
            }
            """,
            "F0004");

    [Fact]
    public async Task Merge_Implemented_Interfaces()
        => await Succeed(
                """
                interface Node {
                  id: ID!
                }

                interface Interface1 implements Node {
                  id: ID!
                  field1(a: String!): String
                }
                """,
                """
                interface Interface1 {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1 implements Node
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1(a: String!): String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                  id: ID!
                    @source(subgraph: "A")
                }

                interface Node
                  @source(subgraph: "A") {
                  id: ID!
                    @source(subgraph: "A")
                }
                """);

    [Fact]
    public async Task Merge_Implemented_Interfaces_When_The_Same()
        => await Succeed(
                """
                interface Node {
                  id: ID!
                }

                interface Interface1 implements Node {
                  id: ID!
                  field1(a: String!): String
                }
                """,
                """
                interface Node {
                  id: ID!
                }

                interface Interface1 implements Node {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1 implements Node
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1(a: String!): String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                  id: ID!
                    @source(subgraph: "A")
                }

                interface Node
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  id: ID!
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Merge_Implemented_Interfaces_When_Different()
        => await Succeed(
                """
                interface Node1 {
                  id: ID!
                }

                interface Interface1 implements Node1 {
                  id: ID!
                  field1(a: String!): String
                }
                """,
                """
                interface Node2 {
                  id: ID!
                }

                interface Interface1 implements Node2 {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                interface Interface1 implements Node1 & Node2
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1(a: String!): String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                  id: ID!
                    @source(subgraph: "A")
                }

                interface Node1
                  @source(subgraph: "A") {
                  id: ID!
                    @source(subgraph: "A")
                }

                interface Node2
                  @source(subgraph: "B") {
                  id: ID!
                    @source(subgraph: "B")
                }
                """);
}