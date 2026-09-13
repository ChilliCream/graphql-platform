namespace HotChocolate.Types;

public class BatchResolverOnMutationFieldAnalyzerTests
{
    [Fact]
    public async Task BatchResolver_Should_RaiseError_When_HostedOnMutationTypeClass()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [MutationType]
            public static partial class Mutation
            {
                [BatchResolver]
                public static List<string> RenameProducts(List<int> id, List<string> name)
                    => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_RaiseError_When_MethodHasMutationAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            public static class Mutation
            {
                [Mutation]
                [BatchResolver]
                public static List<string> RenameProducts(List<int> id, List<string> name)
                    => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_NotRaiseError_When_HostedOnQueryTypeClass()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [QueryType]
            public static partial class Query
            {
                [BatchResolver]
                public static List<string> GetProductNames(List<int> id)
                    => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MutationTypeClass_Should_NotRaiseError_When_MethodHasNoBatchResolver()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [MutationType]
            public static partial class Mutation
            {
                public static string RenameProduct(int id, string name)
                    => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MutationAttributeMethod_Should_NotRaiseError_When_MethodHasNoBatchResolver()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public static class Mutation
            {
                [Mutation]
                public static string RenameProduct(int id, string name)
                    => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
