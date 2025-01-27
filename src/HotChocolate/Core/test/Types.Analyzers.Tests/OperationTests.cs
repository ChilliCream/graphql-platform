namespace HotChocolate.Types;

public class OperationTests
{
    [Fact]
    public async Task Generate_Query_Resolvers()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public static partial class Query
            {
                public static int GetTest(string arg)
                {
                    return arg.Length;
                }
            }
            """).MatchMarkdownAsync();
    }
}
