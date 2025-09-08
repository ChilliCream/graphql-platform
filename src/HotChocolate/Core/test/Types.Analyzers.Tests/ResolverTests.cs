namespace HotChocolate.Types;

public class ResolverTests
{
    [Fact]
    public async Task GenerateSource_ResolverWithLocalStateArgument_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static int GetTest([LocalState("Test")] int test)
                {
                    return test;
                }
            }

            internal class Test;
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_ResolverWithScopedStateArgument_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static int GetTest([ScopedState("Test")] int test)
                {
                    return test;
                }
            }

            internal class Test;
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_ResolverWithGlobalStateArgument_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static int GetTest([GlobalState("Test")] int test)
                {
                    return test;
                }
            }

            internal class Test;
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_ResolverWithLocalStateSetStateArgument_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static int GetTest([LocalState] SetState<int> test)
                {
                    test(1);
                    return 1;
                }
            }

            internal class Test;
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_ResolverWithScopedStateSetStateArgument_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static int GetTest([ScopedState] SetState<int> test)
                {
                    test(1);
                    return 1;
                }
            }

            internal class Test;
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_ResolverWithGlobalStateSetStateArgument_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static int GetTest([GlobalState] SetState<int> test)
                {
                    test(1);
                    return 1;
                }
            }

            internal class Test;
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Inject_QueryContext()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using GreenDonut.Data;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static IQueryable<Entity> GetTest(QueryContext<Entity> test)
                {
                    return default;
                }
            }

            internal class Test;

            internal class Entity;
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Ensure_Entity_Becomes_Node()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                public static Task<Test?> GetTestById([ID<Test>] int id)
                    => Task.FromResult<Test?>(null);
            }

            [ObjectType<Test>]
            internal static partial class TestType
            {
                [NodeResolver]
                public static Task<Test?> GetTest(int id)
                    => Task.FromResult<Test?>(null);
            }

            internal class Test
            {
                public int Id { get; set; }

                public string Name { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Ensure_Entity_Becomes_Node_With_Query_Node_Resolver()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [NodeResolver]
                public static Task<Test?> GetTestById(int id)
                    => Task.FromResult<Test?>(null);
            }

            [ObjectType<Test>]
            internal static partial class TestType
            {
                public static Task<Test?> GetTest(int id)
                    => Task.FromResult<Test?>(null);
            }

            internal class Test
            {
                public int Id { get; set; }

                public string Name { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Internal_NodeResolver_Should_Generate_Source()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Test>]
            internal static partial class TestType
            {
                [NodeResolver]
                internal static Task<Test?> GetTestByIdAsync(int id)
                    => Task.FromResult<Test?>(null);
            }

            internal class Test
            {
                public int Id { get; set; }

                public string Name { get; set; }
            }
            """).MatchMarkdownAsync();
    }
}
