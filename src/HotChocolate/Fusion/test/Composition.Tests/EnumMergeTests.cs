using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class EnumMergeTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Output_Enum()
        => await Succeed(
            """
            type Query {
              field1: Enum1!
            }

            enum Enum1 {
              BAR
            }
            """,
            """
            type Query {
              field1: Enum1!
            }

            enum Enum1 {
              BAZ
            }
            """);
}
