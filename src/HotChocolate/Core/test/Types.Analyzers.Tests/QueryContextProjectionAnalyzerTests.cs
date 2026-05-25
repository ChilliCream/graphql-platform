namespace HotChocolate.Types;

public class QueryContextProjectionAnalyzerTests
{
    [Fact]
    public async Task QueryContext_WithUseProjection_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UsePaging]
                [UseProjection]
                [UseFiltering]
                [UseSorting]
                public static async Task<Connection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => await productService
                        .GetProductsAsync(pagingArgs, query, cancellationToken)
                        .ToConnectionAsync();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task QueryContext_WithoutUseProjection_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UsePaging]
                [UseFiltering]
                [UseSorting]
                public static async Task<Connection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => await productService
                        .GetProductsAsync(pagingArgs, query, cancellationToken)
                        .ToConnectionAsync();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task UseProjection_WithoutQueryContext_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UsePaging]
                [UseProjection]
                [UseFiltering]
                [UseSorting]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task QueryContext_WithUseProjections_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UsePaging]
                [UseProjections]
                [UseFiltering]
                [UseSorting]
                public static async Task<Connection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => await productService
                        .GetProductsAsync(pagingArgs, query, cancellationToken)
                        .ToConnectionAsync();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task QueryContext_MultipleAttributes_OnlyUseProjectionFlagged()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UsePaging]
                [UseProjection]
                [UseFiltering]
                [UseSorting]
                public static async Task<Connection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => await productService
                        .GetProductsAsync(pagingArgs, query, cancellationToken)
                        .ToConnectionAsync();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoQueryContext_NoAttributes_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
