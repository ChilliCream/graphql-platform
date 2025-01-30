namespace HotChocolate.Types;

public class OperationTests
{
    [Fact]
    public async Task Partial_Static_QueryType()
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

    [Fact]
    public async Task Static_QueryType()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public static class Query
            {
                public static int GetTest(string arg)
                {
                    return arg.Length;
                }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Instance_QueryType()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public class Query
            {
                public static int GetTest(string arg)
                {
                    return arg.Length;
                }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Root_Projection_Single_Entity()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using GreenDonut.Data;

            namespace TestNamespace;

            [QueryType]
            public static partial class Query
            {
                public static Foo GetTest(QueryContext<Foo> context)
                {
                    return new Foo { Bar = ""abc"" };
                }
            }

            public class Foo
            {
                public string Bar { get; set; }
            }
            """).MatchMarkdownAsync();
    }
}
