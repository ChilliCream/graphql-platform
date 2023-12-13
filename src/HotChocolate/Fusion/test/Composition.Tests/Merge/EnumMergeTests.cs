using CookieCrumble;
using HotChocolate.Fusion.Composition.Pipeline;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class EnumMergeTests(ITestOutputHelper output)
    : CompositionTestBase(output, new EnumTypeMergeHandler())
{
    [Fact]
    public async Task Identical_Enums_Merge()
        => await Succeed(
                """
                enum Enum1 {
                  BAR
                }
                """,
                """
                enum Enum1 {
                  BAR
                }
                """)
            .MatchInlineSnapshotAsync(
                """"
                enum Enum1
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  BAR
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                """");

    [Fact]
    public async Task Enums_Do_Not_Merge_When_Values_Differ()
        => await Fail(
            """
            enum Enum1 {
              BAR
            }
            """,
            """
            enum Enum1 {
              BAZ
            }
            """,
            "F0003");

    [Fact]
    public async Task Types_With_The_Same_Name_Must_Be_Of_The_Same_Kind()
        => await Fail(
            """
            enum Enum1 {
              BAR
            }
            """,
            """
            scalar Enum1
            """,
            "F0001");
}