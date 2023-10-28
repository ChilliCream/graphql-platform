using CookieCrumble;
using HotChocolate.Fusion.Composition.Pipeline;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class ObjectMergeTests(ITestOutputHelper output)
    : CompositionTestBase(
        output,
        new ObjectTypeMergeHandler(),
        new InterfaceTypeMergeHandler(),
        new ScalarTypeMergeHandler())
{
    [Fact]
    public async Task Identical_Objects_Merge()
        => await Succeed(
                """
                type Person {
                  field1: String!
                }
                """,
                """
                type Person {
                  field1: String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Person
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Objects_With_Different_Fields_Merge()
        => await Succeed(
                """
                type Person {
                  field1: String!
                }
                """,
                """
                type Person {
                  field2: Int!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Person
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
                type Person {
                  field1: String!
                }
                """,
                """
                type Person {
                  field1: String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Person
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
            type Person {
              field1: String
            }
            """,
            """
            type Person {
              field1: Int
            }
            """,
            "F0002");

    [Fact]
    public async Task Types_With_The_Same_Name_Must_Be_Of_The_Same_Kind()
        => await Fail(
            """
            type Person {
              field1: Enum1!
            }

            enum Enum1 {
              BAR
            }
            """,
            """
            type Person {
              field1: Enum1!
            }

            scalar Enum1
            """,
            "F0002");

    [Fact]
    public async Task Fields_Merge_When_Arguments_Are_Identical()
        => await Succeed(
                """
                type Person {
                  field1(a: String): String
                }
                """,
                """
                type Person {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Person
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
                type Person {
                  field1(a: String!): String
                }
                """,
                """
                type Person {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Person
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
            type Person {
              field1(a: String): String
            }
            """,
            """
            type Person {
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

                type User implements Node {
                  id: ID!
                  field1(a: String!): String
                }
                """,
                """
                type User {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type User implements Node
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

                type User implements Node {
                  id: ID!
                  field1(a: String!): String
                }
                """,
                """
                interface Node {
                  id: ID!
                }

                type User implements Node {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type User implements Node
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

                type User implements Node1 {
                  id: ID!
                  field1(a: String!): String
                }
                """,
                """
                interface Node2 {
                  id: ID!
                }

                type User implements Node2 {
                  field1(a: String): String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type User implements Node1 & Node2
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