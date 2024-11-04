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
}
