namespace HotChocolate.Types;

public class RootTypePartialAnalyzerTests
{
    [Fact]
    public async Task StaticPartialQueryType_NoDiagnostic()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public static partial class Query
            {
                public static string Hello() => "world";
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NonStaticPartialQueryType_NoDiagnostic()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public partial class Query
            {
                public string Hello() => "world";
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task StaticNonPartialQueryType_RaisesInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public static class Query
            {
                public static string Hello() => "world";
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NonStaticNonPartialQueryType_RaisesInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public class Query
            {
                public string Hello() => "world";
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NonStaticPartialQueryType_InstanceBatchResolver_UsesResolverReceiver()
    {
        // The generated batch resolver must call the instance method through
        // contexts[0].Resolver<Query>() rather than Query.GetGreeting(...), which
        // would not compile for an instance method on a non-static class.
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System.Collections.Generic;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            public partial class Query
            {
                [BatchResolver]
                public List<string> GetGreeting([Parent] List<Query> roots)
                    => default!;
            }
            """]).MatchMarkdownAsync();
    }
}
