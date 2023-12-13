using CookieCrumble;
using HotChocolate.Fusion.Composition.Pipeline;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class InputMergeTests(ITestOutputHelper output)
    : CompositionTestBase(output, new InputObjectTypeMergeHandler(), new ScalarTypeMergeHandler())
{
    [Fact]
    public async Task Identical_Inputs_Merge()
        => await Succeed(
                """
                input Input1 {
                  field1: String
                }
                """,
                """
                input Input1 {
                  field1: String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                input Input1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1: String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Fields_With_Different_Nullability_Will_Merge()
        => await Succeed(
                """
                input Input1 {
                  field1: String!
                }
                """,
                """
                input Input1 {
                  field1: String
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                input Input1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """);

    [Fact]
    public async Task Fields_With_Different_Types_Will_Not_Merge()
        => await Fail(
            """
            input Input1 {
              field1: Int
            }
            """,
            """
            input Input1 {
              field1: String
            }
            """,
            "F0005");

    [Fact]
    public async Task Input_Types_With_Different_Fields_Across_Subgraphs_Will_Not_Merge()
        => await Fail(
            """
            input Input1 {
              field1: String
            }
            """,
            """
            input Input1 {
              field2: String
            }
            """,
            "F0006");
}