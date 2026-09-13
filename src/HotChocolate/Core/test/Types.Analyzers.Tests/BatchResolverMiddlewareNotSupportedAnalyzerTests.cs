namespace HotChocolate.Types;

public class BatchResolverMiddlewareNotSupportedAnalyzerTests
{
    [Fact]
    public async Task BatchResolver_Should_RaiseError_When_MethodHasUseDataLoaderAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [BatchResolver]
                [UseDataLoader(typeof(object))]
                public static List<string> GetDisplayName([Parent] List<Product> products)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_RaiseError_When_MethodHasUseFirstOrDefaultAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [BatchResolver]
                [UseFirstOrDefault]
                public static List<string> GetDisplayName([Parent] List<Product> products)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_RaiseError_When_MethodHasUseSingleOrDefaultAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [BatchResolver]
                [UseSingleOrDefault]
                public static List<string> GetDisplayName([Parent] List<Product> products)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_RaiseErrorOncePerAttribute_When_MethodHasMultiplePerParentMiddlewareAttributes()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [BatchResolver]
                [UseDataLoader(typeof(object))]
                [UseFirstOrDefault]
                public static List<string> GetDisplayName([Parent] List<Product> products)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_NotRaiseError_When_MethodHasUsePagingAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [BatchResolver]
                [UsePaging]
                public static List<string> GetDisplayName([Parent] List<Product> products)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_NotRaiseError_When_MethodHasUseFilteringAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [BatchResolver]
                [UseFiltering]
                public static List<string> GetDisplayName([Parent] List<Product> products)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BatchResolver_Should_NotRaiseError_When_MethodHasAuthorizeAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Authorization;
            using HotChocolate.Types;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [BatchResolver]
                [Authorize]
                public static List<string> GetDisplayName([Parent] List<Product> products)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PerParentResolver_Should_NotRaiseError_When_MethodHasUseDataLoaderAttribute()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UseDataLoader(typeof(object))]
                public static string GetDisplayName([Parent] Product product)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
